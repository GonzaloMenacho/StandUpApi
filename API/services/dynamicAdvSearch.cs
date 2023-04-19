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
        public static List<ISearchResponse<Review>> sortByRevelancy(List<ISearchResponse<Review>> reviewResList)
        {

            return reviewResList;
        }

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

            bool addedReviewKeywordMultiMatch = false;       // flag that tells us if we have already done a multimatch for review keyword

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
                    catch (Exception)
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
                            terms[0] = terms[0].ToLower();
                        }

                        if (p.PropertyType.IsArray) //
                        {
                            Array a = (Array)p.GetValue(form);
                            if (a.Length > 0)
                            {
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
                        }
                        else if (field == "title")
                        {
                            terms = terms[0].Split(' ');
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
                        else if ((field == "Review Title" || field == "Review") && form.ReviewTitle == form.ReviewBody)
                        {
                            if (!addedReviewKeywordMultiMatch)
                            {
                                var q = new QueryContainerDescriptor<T>().Bool(
                                                b => b.Must(multiMatchService.MultiMatchListBuilder(new[] { "Review Title", "Review" }, terms, false)));
                                queryContainerList.Add(q);
                                addedReviewKeywordMultiMatch = true;
                            }
                        }
                        else
                        {
                            var q = new QueryContainerDescriptor<T>().Bool(
                                            b => b.Must(matchService.MatchListBuilder(field, terms, false)));
                            queryContainerList.Add(q);
                        }
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
            List<List<Review>> setOfReviewsList = new List<List<Review>>();
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
                    // but how do we guarentee only 3 reviews per movie?
                    // nested query of multiple size 3 queries?
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

                    if (reviewRes.Documents.Count > 0)
                    {
                        setOfReviewsList.Add(reviewRes.Documents.ToList());
                    }
                }
            } else         // no movie criteria was searched, search on reviews first, then find the movies
            {
                /* Bad Algo:
                 * if movie query null, get all movies
                 * for each movieID, make the review query with size 3. 
                 * for each movieID that returns 0 hits, remove that movie from the list
                 * end result should be only the movies with hits
                 * but not sorted by relevancy of those hits (Sad!)
                 */
                form.MovieTitle = ""; // returns all movies
                var movieRes = await _elasticClient.SearchAsync<Movie>(s => s
                                .Index(MoviesController.movieIndex)
                                .Query(q => query
                                    )
                                );
                movieList = movieRes.Documents.ToList();


                List<ISearchResponse<Review>> reviewResList = new List<ISearchResponse<Review>>();
                foreach (Movie movie in movieList)
                {
                    form.movieID = movie.MovieID.ToString();
                    // ideally should break these out into query containers, then do 1 request
                    // but how do we guarentee only 3 reviews per movie?
                    // nested query of multiple size 3 queries?
                    var reviewRes = await _elasticClient.SearchAsync<Review>(s => s
                                    .Index(ReviewsController.reviewIndex)
                                    .Size(3)
                                    .Query(q => q
                                            .Bool(b =>b
                                                .Must(dynamicAdvSearch
                                                .SingleIndexRequest(new Review(), form)
                                                            )
                                                        )
                                                    )   
                                    );
                    
                    if (reviewRes.Documents.Count > 0)
                    {
                        reviewResList.Add(reviewRes);
                    }
                }

                var orderByRelevance = from result in reviewResList
                                       orderby result.MaxScore descending
                                       select result;


                foreach(ISearchResponse<Review> result in orderByRelevance)
                {
                    setOfReviewsList.Add(result.Documents.ToList());
                }

                //List<int> returnedMovieIDs = new List<int>();
                //foreach (List<Review> reviewSet in setOfReviewsList)
                //{
                //    if (reviewSet.Count > 0)
                //    {
                //        if (!returnedMovieIDs.Contains(reviewSet[0].MovieID))
                //        {
                //            returnedMovieIDs.Add(reviewSet[0].MovieID);
                //        }
                //    }
                //}

                //List<Movie> validMovies = new List<Movie>(movieList);
                //foreach(Movie movie in movieList)
                //{
                //    if (!returnedMovieIDs.Contains(movie.MovieID))
                //    {
                //        validMovies.Remove(movie);
                //    }
                //}

                //movieList = validMovies;
                movieList = new List<Movie>(); // return empty list if no movie hits
            }
            MovieReview results = new MovieReview();
            results.MovieDocuments = movieList;
            results.ReviewDocuments = setOfReviewsList;

            return results;
        }
    }
}
