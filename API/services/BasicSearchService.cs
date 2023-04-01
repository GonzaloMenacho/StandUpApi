using Microsoft.AspNetCore.Mvc;
using Nest;

namespace API.services
{
    public class BasicSearchService
    {
        public static async Task<ActionResult<MovieReview>> GetMovieReviewFromTerm(IElasticClient _elasticClient, string? term = null)
        {
            List<Movie> movielist = new List<Movie>();
            List<List<Review>> reviewlist = new List<List<Review>>();

            if (term == null)
            {
                // get all the movie documents in the db
                // returns 10 movies
                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index("movies")
                    .Size(10)
                    .Query(q => q.MatchAll())
                    );
                if (!response.IsValid)  // if we didn't reach the database for the movies
                {
                    throw new HttpRequestException("Failed to MatchAll movies in BasicSearchService.");
                }
                movielist = response.Documents.ToList();

                Review reviewOBJ = new Review();
                foreach (Movie movie in movielist) // for each movie in our movie list
                {
                    string[] movieidarr = { movie.MovieID.ToString() };
                    Console.WriteLine(movieidarr);
                    var res = await _elasticClient.SearchAsync<Review>(s => s
                        .Index("reviews")   // https://tenor.com/view/now-whos-gonna-stop-me-bowser-the-super-mario-bros-movie-try-to-stop-me-now-im-unstoppable-gif-26913745
                        .Size(3)            // limits the number of hits to 3
                        .Query(q => matchService.MatchRequest("movieID", reviewOBJ, movieidarr))
                        .Sort(sort => sort
                            .Descending("_score")           // this sorts by relevancy
                        //.Descending(f => f.UsefulnessVote)    // this sorts by usefulness votes
                        )
                        );
                    if (!res.IsValid)   // if we didn't get the review json back
                    {
                        throw new HttpRequestException("Failed to retrieve reviews in BasicSearchService with an empty string.");
                    }
                    reviewlist.Add(res.Documents.ToList()); // append the response to the movie list.
                }
            }
            else
            {
                // search movies //
                Movie movieobj = new Movie();
                string[] termsList = { term };      // unecessarily needed, but absolutely required
                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index("movies")
                    .Query(q =>
                        searchByCharRaw.RegexpRequest("title", movieobj, termsList)
                        )
                    );
                if (!response.IsValid)
                {
                    throw new HttpRequestException("Failed to Regexp match movies in BasicSearchService.");
                }
                movielist = response.Documents.ToList();

                if (movielist.Count == 0)
                {
                    // get all the movie documents in the db
                    // returns 10 movies
                    var res = await _elasticClient.SearchAsync<Movie>(s => s
                        .Index("movies")
                        .Size(10)
                        .Query(q => q
                            .MatchAll()
                            )
                        );
                    if (!res.IsValid)  // if we didn't reach the database for the movies
                    {
                        throw new HttpRequestException("Failed to MatchAll movies in BasicSearchService after finding no results using term.");
                    }
                    movielist = res.Documents.ToList();
                }

                // search reviews //

                // if we have movies, then grab 3 reviews for each movie
                if (movielist.Count > 0)
                {
                    foreach (Movie movie in movielist)
                    {
                        var res = await _elasticClient.SearchAsync<Review>(s => s
                            .Index("reviews")
                            .Query(q => WeightedReviewQuery(term, movie))
                            .Size(3)
                            .Sort(sort => sort
                                .Descending("_score")           // this sorts by relevancy
                                //.Descending(f => f.UsefulnessVote)    // this sorts by usefulness votes
                                )
                            );

                        if (!res.IsValid)
                        {
                            throw new HttpRequestException("Failed to find reviews in BasicSearchService function.");
                        }
                        if (res.Documents.Any())    // if we get no reviews, don't add to the list
                        {
                            reviewlist.Add(res.Documents.ToList());
                        }

                    }
                }
            }

            // return the moviereview object
            MovieReview movieReview = new MovieReview
            {
                MovieDocuments = movielist,
                ReviewDocuments = reviewlist
            };
            return movieReview;   // return the MR object with status 200
        }

        public static QueryContainer WeightedReviewQuery(string term, Movie movie)
        {
            return Query<Review>.FunctionScore(fs => fs
                .Query(q2 => q2
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(doc => doc.ReviewTitle, boost: 2.5)
                            .Field(doc => doc.ReviewBody)
                        )
                        .Query(term)
                        .Slop(2)
                        .Fuzziness(Fuzziness.Auto)
                        .PrefixLength(2)
                        .MaxExpansions(1)
                        .Operator(Operator.Or)
                        .Name($"s?={term}")
                        .AutoGenerateSynonymsPhraseQuery(false)
                    ) && q2.Term(t => t
                        .Field(f => f.MovieID)
                        .Value(movie.MovieID)
                    )
                )
                .BoostMode(FunctionBoostMode.Multiply)
                .ScoreMode(FunctionScoreMode.Sum)
                .Functions(f => f
                    .Exponential(d => d
                        .Field(f => f.UsefulnessVote)
                        .Decay(0.33)
                        .Origin(5000)
                        .Scale(5)
                        .Offset(1)
                        .Weight(0.1)
                    )
                )
            );
        }
    }
}


/*
             try
            {
                var movieReview = await BasicSearchService.GetMovieReviewFromTerm(_elasticClient, term);
                return Ok(movieReview.Value);   // return the MR object with status 200
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
*/

/*
                            .Query(q => q
                                .FunctionScore(fs => fs     //this lets us return better results
                                    .Query(q2 => q2
                                        .MultiMatch(m => m
                                            .Fields(f => f  // search the term on these fields:
                                                .Field(doc => doc.ReviewTitle, boost: 2.5)      // add boosting to the title
                                                .Field(doc => doc.ReviewBody)
                                            )
                                            .Query(term.ToString())     // the term we're querying
                                            .Slop(2)                    // character misplacements (typos: avengers=>aevngers)
                                            .Fuzziness(Fuzziness.Auto)  // token distance (tomato soup=>soup tomatos)
                                            .PrefixLength(2)            // allows prefixes (
                                            .MaxExpansions(1)          // limits the amount of docs returned
                                            .Operator(Operator.Or)
                                            .Name($"s?={term}")
                                            .AutoGenerateSynonymsPhraseQuery(false)
                                            ) && q      // this lets us grab movies based on movieID
                                            .Term(t => t
                                                .Field(f => f.MovieID)
                                                .Value(movie.MovieID)
                                            )
                                        )
                                    .BoostMode(FunctionBoostMode.Multiply)  // boosting settings
                                    .ScoreMode(FunctionScoreMode.Sum)       // score settings
                                    .Functions(f => f           // ngl, chatgpt moment
                                        .Exponential(d => d     // this basically says add a bit of boosting to reviews with higher relevance scores
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
*/

/*

                            .Query(q =>
                                WeightedReviewQuery(term, movie)
                                )


*/