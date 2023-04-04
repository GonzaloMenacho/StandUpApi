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
        private static QueryContainer[] QueryListBuilder(AdvancedSearchForm form, bool searchingMovies)
        {
            //QueryContainer orQuery = null;
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

                        // movieID on the form is a review attribute. this means we are entering review attributes which we can ignore for movie searches
                        if (searchingMovies && field == "movieID")
                        { break; }
                    }
                    catch (Exception e)
                    {
                        if (searchingMovies)
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
                            queryContainerList.AddRange(minMaxService.RangeQueryBuilder(field, (float)a.GetValue(0), (float)a.GetValue(1)));
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
                            queryContainerList.AddRange(matchService.MatchListBuilder(field, s, false));
                        }
                    }
                    else if (field == "title")
                    {
                        queryContainerList.AddRange(searchByCharRaw.RegexpListBuilder(field, terms));
                    }
                    else if (field == "movieID" && !searchingMovies) // movieID is a fucking int in the reviewIndex specifically
                    {
                        float movieIDNum = float.Parse(terms[0], CultureInfo.InvariantCulture.NumberFormat);
                        queryContainerList.AddRange(minMaxService.RangeQueryBuilder(field, movieIDNum, movieIDNum));
                    }
                    else
                    {
                        queryContainerList.AddRange(matchService.MatchListBuilder(field, terms, false));
                    }

                    // to make things smoother when the same form is ran through this twice for different indexes
                    p.SetValue(form, null);
                }
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer SingleIndexRequest<T>(T typeOBJ, AdvancedSearchForm form) where T : class
        {
            bool searchingMovie;
            if (typeOBJ is Movie)
            {
                searchingMovie = true;
            }
            else
            {
                searchingMovie = false;
            }

            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Must(
                    QueryListBuilder(form, searchingMovie)));
            return q;
        }

        public async static Task<ActionResult<MovieReview>> BothIndexRequest(IElasticClient _elasticClient, AdvancedSearchForm form)
        {
            List<Movie> movieList = new List<Movie>();
            List<List<Review>> reviewList = new List<List<Review>>();
            var movieRes = await _elasticClient.SearchAsync<Movie>(s => s
                            .Index(MoviesController.movieIndex)
                            .Query(q => dynamicAdvSearch.SingleIndexRequest(new Movie(), form)
                                )
                            );
            movieList = movieRes.Documents.ToList();
            foreach (Movie movie in movieList)
            {
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
            MovieReview results = new MovieReview();
            results.MovieDocuments = movieList;
            results.ReviewDocuments = reviewList;

            return results;
        }
    }
}
