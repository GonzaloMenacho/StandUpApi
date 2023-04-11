using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System.Globalization;
using System.Reflection;

namespace API.services
{
    //TODO: add some way to handle both movies and reviews in one advanced search. How do we grab only reviews of movies that meet our specified criteria?
    // Ideas: search on the movie criteria first, then add a query on the reviews that says "grab only reviews from these movieIDs"
    public class dynamicAdvSearch
    {

        public static QueryContainer SingleIndexRequest<T>(T typeOBJ, AdvancedSearchForm form) where T : class
        {
            bool searchingMovies;
            if (typeOBJ is Movie)
            {
                searchingMovies = true;
            }
            else
            {
                searchingMovies = false;
            }

            //  QUERY LIST BUILDER
            // no longer its own function so we can decide when to use should or must per field
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            Type classType = form.GetType();
            PropertyInfo[] props = classType.GetProperties();

            foreach (PropertyInfo p in props)
            {
                if (p.GetValue(form) != null)
                {
                    // Get name of the elastic field
                    string field = p.Name;
                    string suffix = "MinMax";
                    if (field.EndsWith(suffix))
                    {
                        field = field.Substring(0, field.Length - suffix.Length);
                    }
                    try
                    {
                        field = MoviesController.MovieFields[field.ToLower().Trim()];

                        if (searchingMovies && field == "movieID") // movieID on the form is a review attribute. this means we are entering review attributes which we can ignore for movie searches
                        { break; }
                        else if (!searchingMovies && field == "movieID") // i.e., we only want to do review adv search
                        {
                            throw new Exception();
                        }
                        else if (!searchingMovies) // we are in review search and was given a form with movie attribute filled
                        {
                            p.SetValue(form, null); // get rid of the irrelevant info on the form
                        }
                    }
                    catch (Exception e)
                    {
                        if (searchingMovies) // we are in movie search about to analyze review attributes
                        { break; }
                        try
                        {
                            field = ReviewsController.ReviewFields[field.ToLower().Trim()];
                        }
                        catch (Exception e2)
                        {
                            Console.WriteLine("Failed to set name of attribute at elastic field " + e2);
                        }
                    }

                    if (p.GetValue(form) != null)
                    {
                        // Begin parsing properties and building queries
                        string[] terms = { "" };
                        //Type propType = p.GetType();
                        if (p.GetValue(form) is string)
                        {
                            terms[0] = (string)p.GetValue(form);
                        }

                        if (p.PropertyType.IsArray) //
                        {
                            Array a = (Array)p.GetValue(form);
                            if (a.GetValue(0) is float)  // numeric array == min max
                            {
                                var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(minMaxService.RangeQueryBuilder(field, (float)a.GetValue(0), (float)a.GetValue(1))));
                                queryContainerList.Add(q);
                            }
                            else // array of strings? 
                            {
                                string arrayElements = "";

                                for (int i = 0; i < a.Length; i++)
                                {
                                    object pValue = a.GetValue(i);
                                    arrayElements += pValue.ToString() + " ";
                                }
                                string[] s = { arrayElements };
                                var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(matchService.MatchListBuilder(field, s, false)));
                                queryContainerList.Add(q);
                            }
                        }
                        else if (field == "title")
                        {
                            var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(searchByCharRaw.RegexpListBuilder(field, terms)));
                            queryContainerList.Add(q);
                        }
                        else if (field == "movieID" && !searchingMovies) // movieID is a fucking int in the reviewIndex specifically
                        {
                            float movieIDNum = float.Parse(terms[0], CultureInfo.InvariantCulture.NumberFormat);
                            var q = new QueryContainerDescriptor<T>().Bool(
                                             b => b.Must((minMaxService.RangeQueryBuilder(field, movieIDNum, movieIDNum))));
                            queryContainerList.Add(q);
                        }
                        else if (field == "Review Title" || field == "Review")
                        {
                            // should is too lenient perhaps, but we want to allow a document to have the terms
                            // in either field. remove stop words?
                            var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(matchService.MatchListBuilder(field, terms)));
                            queryContainerList.Add(q);
                        }
                        else
                        {
                            var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(matchService.MatchListBuilder(field, terms, false)));
                            queryContainerList.Add(q);
                        }

                        // to make things smoother when the same form is ran through this twice for different indexes
                        p.SetValue(form, null);
                    }
                }
            }
            if (queryContainerList.Count() == 0)
            {
                return null;
            }

            var finalq = new QueryContainerDescriptor<T>().Bool(
                b => b.Must(queryContainerList.ToArray()));
            return finalq;
        }

        public async static Task<ActionResult<MovieReview>> BothIndexRequest(IElasticClient _elasticClient, AdvancedSearchForm form)
        {
            List<Movie> movieList = new List<Movie>();
            List<List<Review>> reviewList = new List<List<Review>>();
            if (ObjectAnalyzer.IsAllNullOrEmpty(form))
            {
                form.MovieTitle = ""; // returns all movies
            }

            var query = dynamicAdvSearch.SingleIndexRequest(new Movie(), form);
            if (query != null)
            {
                var movieRes = await _elasticClient.SearchAsync<Movie>(s => s
                                .Index(MoviesController.movieIndex)
                                .Query(q => query
                                    )
                                );
                movieList = movieRes.Documents.ToList();
                foreach (Movie movie in movieList)
                {
                    // ideally should break these out into query containers, then do 1 request
                    form.movieID = movie.MovieID.ToString();
                    var reviewRes = await _elasticClient.SearchAsync<Review>(s => s
                                    .Index(ReviewsController.reviewIndex)
                                    .Size(3)
                                    .Query(q => q
                                        .FunctionScore(fs => fs
                                            .Query(q2 => dynamicAdvSearch
                                                .SingleIndexRequest(new Review(), form))
                                                    .BoostMode(FunctionBoostMode.Multiply)
                                                    .ScoreMode(FunctionScoreMode.Sum)
                                                    .Functions(f => f
                                                        .Exponential(d => d         // higher usefulness votes = higher boosting
                                                            .Field(f => f.UsefulnessVote)
                                                            .Decay(0.33)
                                                            .Origin(5000)
                                                            .Scale(5)
                                                            .Offset(1)
                                                            .Weight(0.1)
                                                    )
                                                )
                                            )
                                        )
                                    );
                    reviewList.Add(reviewRes.Documents.ToList());
                }
            } else         // no movie criteria was searched, search on reviews first, then find the movies
            {
                //TODO: cant figure out how to write a query that returns 3 reviews per unique movieID
                // ultra slow search, dont worry about it, trust the plan etc
                /* Proposed Algo:
                 * if movie results null, get all movies
                 * for each movie, make the review
                 */
                var reviewRes = await _elasticClient.SearchAsync<Review>(s => s
                                    .Index(ReviewsController.reviewIndex)
                                    .Size(5000)
                                    .Query(q => q
                                        .FunctionScore(fs => fs
                                            .Query(q2 => dynamicAdvSearch
                                                .SingleIndexRequest(new Review(), form))
                                                    .BoostMode(FunctionBoostMode.Multiply)
                                                    .ScoreMode(FunctionScoreMode.Sum)
                                                    .Functions(f => f
                                                        .Exponential(d => d         // higher usefulness votes = higher boosting
                                                            .Field(f => f.UsefulnessVote)
                                                            .Decay(0.33)
                                                            .Origin(5000)
                                                            .Scale(5)
                                                            .Offset(1)
                                                            .Weight(0.1)
                                                    )
                                                )
                                            )
                                        )
                                    );
                /*
                 * var reviewRes = await _elasticClient.SearchAsync<Review>(s => s
                                    .Index(ReviewsController.reviewIndex)
                                    .Size(30)
                                    .Query(q => q
                                        .FunctionScore(fs => fs
                                            .Query(q2 => dynamicAdvSearch
                                                .SingleIndexRequest(new Review(), form))
                                                    .BoostMode(FunctionBoostMode.Multiply)
                                                    .ScoreMode(FunctionScoreMode.Sum)
                                                    .Functions(f => f
                                                        .Exponential(d => d         // higher usefulness votes = higher boosting
                                                            .Field(f => f.UsefulnessVote)
                                                            .Decay(0.33)
                                                            .Origin(5000)
                                                            .Scale(5)
                                                            .Offset(1)
                                                            .Weight(0.1)
                                                    )
                                                )
                                            )
                                        )
                                    .Collapse(c => c
                                        .Field(f=>f.MovieID)
                                        .InnerHits(i => i
                                            .Name("3perMovie")
                                            .Size(3)
                                            .Source(s=>s
                                                .Includes(i=>i 
                                                    .Field(f => f.MovieID)
                                                    )
                                                )
                                            )
                                        .MaxConcurrentGroupSearches(10)
                                        )
                                    );
                /*
                 * .Aggregations(agg=>agg
                                    .Terms("movieID", t=>t
                                        .Field(f=>f.MovieID)
                                        .Size(10)
                                        ) 
                                    )
                                    .Aggregations(agg2 => agg2
                                        .TopHits("3perMovie", th=>th
                                            .Size(3)
                                            )
                                        )
                 * */
                List<string> knownIDs = new List<string>();
                foreach (Review review in reviewRes.Documents.ToList())
                {
                    if(!knownIDs.Contains(review.MovieID.ToString())){
                        knownIDs.Add(review.MovieID.ToString());
                        List<Review> thisMovie = new List<Review>();
                        thisMovie.Add(review);
                        reviewList.Add(thisMovie);
                    }
                    else
                    {
                        foreach (List<Review> movieRevs in reviewList)
                        {
                            if (movieRevs[0].MovieID == review.MovieID && movieRevs.Count < 3)
                            {
                                movieRevs.Add(review);
                            }
                        }
                    }
                }

                var movieRes = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(MoviesController.movieIndex)
                .Query(q => matchService
                    .MatchRequest("movieID", new Movie(), knownIDs.ToArray())
                    )
                );
                movieList = movieRes.Documents.ToList();
                var sortedMovieList = new List<Movie>();

                // resort movie list to match the relevancy of the reviews
                foreach(string ID in knownIDs)
                {
                    foreach(Movie movie in movieList)
                    {
                        if (movie.MovieID.ToString() == ID)
                        {
                            sortedMovieList.Add(movie);
                        }
                    }
                }
                movieList = sortedMovieList;
            }
            MovieReview results = new MovieReview();
            results.MovieDocuments = movieList;
            results.ReviewDocuments = reviewList;

            return results;
        }
    }
}
