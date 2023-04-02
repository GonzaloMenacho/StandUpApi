using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Threading.Tasks;

namespace API.services
{
    public class BasicSearchService
    {
        private static readonly string movieIndex = "movies";
        private static readonly string reviewIndex = "reviews";

        public static async Task<ActionResult<MovieReview>> GetMovieReviewFromTerm(IElasticClient _elasticClient, string? term = null)
        {
            List<Movie> movielist = new List<Movie>();
            List<List<Review>> reviewlist = new List<List<Review>>();

            if (term == null)
            {
                // get all the movie documents in the db
                // returns 10 movies
                try
                {
                    var res = MatchAllGetMovieList(_elasticClient);
                    movielist = res.Result;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                Review reviewOBJ = new Review();
                foreach (Movie movie in movielist) // for each movie in our movie list
                {
                    string[] movieidarr = { movie.MovieID.ToString() };
                    var res = await MatchSearchQuery(_elasticClient, new Review(), "movieID", movieidarr, 3);

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
                    .Index(movieIndex)
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
                    try
                    {
                        var res = MatchAllGetMovieList(_elasticClient);
                        movielist = res.Result;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }

                // search reviews //

                // if we have movies, then grab 3 reviews for each movie
                if (movielist.Count > 0)
                {
                    foreach (Movie movie in movielist)
                    {
                        var res = await GetWeightedReviewQuery(_elasticClient, movie, term, reviewIndex, 3);
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

        // get all the movie documents in the db
        // defaults to 10
        // returns as List<Movie>
        public static async Task<List<Movie>> MatchAllGetMovieList(IElasticClient _elasticClient, int? size = 10, string? index = "movies")
        {
            List<Movie> movieList = new List<Movie>();  
            var response = await MatchAllQuery(_elasticClient, index, size);
            if (!response.IsValid)  // if we didn't reach the database for the movies
            {
                throw new HttpRequestException("Failed to get a response in GetAllMovieList.");
            }
            movieList = response.Documents.ToList();
            return movieList;
        }

        // Ease of access for MatchAll Query
        // Specify index "movies" or "reviews"
        // Specify size to determine how many documents are returned
        public static async Task<ISearchResponse<Movie>> MatchAllQuery(IElasticClient _elasticClient, string index, int? size = 10)
        {
            if (_elasticClient is null)
            {
                throw new ArgumentNullException(nameof(_elasticClient));
            }

            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(index)
                .Size(size)
                .Query(q => q
                    .MatchAll())
                );
            if (!response.IsValid)  // if we didn't reach the database for the movies
            {
                throw new HttpRequestException("Failed to get a response in MatchAllQuery.");
            }
            return response;
        }

        // Get response for a specific Match query
        public static async Task<ISearchResponse<T>> MatchSearchQuery<T>(
            IElasticClient _elasticClient,
            T typeOBJ,
            string field,
            string[] termArray,
            int? size = 10,
            string? sortorder = "_score") where T : class
        {
            if (_elasticClient is null)
            {
                throw new ArgumentNullException(nameof(_elasticClient));
            }

            if (typeOBJ is null)
            {
                throw new ArgumentNullException(nameof(typeOBJ));
            }

            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (termArray is null)
            {
                throw new ArgumentNullException(nameof(termArray));
            }

            object ModelOBJ = null;
            string index = null;
            switch (typeOBJ)
            {
                case Movie: ModelOBJ = new Movie(); index = movieIndex; break;
                case Review: ModelOBJ = new Review(); index = reviewIndex; break;
                default: throw new ArgumentException("You need to pass in a valid empty object (Movie/Review) into MatchSearchQuery!");
            }

            var response = await _elasticClient.SearchAsync<T>(s => s
                .Index(index)   // https://tenor.com/view/now-whos-gonna-stop-me-bowser-the-super-mario-bros-movie-try-to-stop-me-now-im-unstoppable-gif-26913745
                .Size(size)            // limits the number of hits to 3 if "3" is input
                .Query(q => matchService.MatchRequest(field, ModelOBJ, termArray))
                .Sort(sort => sort
                    .Descending(sortorder)           // this sorts by relevancy if "_score" input
                    )
                );
            if (!response.IsValid)  // if we didn't reach the database for the movies
            {
                throw new HttpRequestException("Failed to get a response in MatchSearchQuery.");
            }
            return response;
        }

        // This performs a match query matching the term to the review index
        // This function also boosts scores with higher usefulness votes
        public static async Task<ISearchResponse<Review>> GetWeightedReviewQuery(
            IElasticClient _elasticClient, 
            Movie movie, 
            string term = "",
            string? index = "reviews", 
            int? size = 3,
            string? sortorder = "_score")
        {
            if (_elasticClient is null)
            {
                throw new ArgumentNullException(nameof(_elasticClient));
            }

            if (movie is null)
            {
                throw new ArgumentNullException(nameof(movie));
            }

            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(index)
                .Query(q => GetWeightedReviewQueryContainer(term, movie))
                .Size(size)
                .Sort(sort => sort
                    .Descending(sortorder)           // this sorts by relevancy
                    //.Descending(f => f.UsefulnessVote)    // this sorts by usefulness votes
                    )
                );
            if (!response.IsValid)  // if we didn't reach the database for the movies
            {
                throw new HttpRequestException("Failed to get a response in GetWeightedReviewQuery.");
            }
            return response;
        }


        // This just helps to build the entire query.
        // For future improvement, parameterize every single query method's arguments
        // movie has to not be an actual movie object, not an empty one
        public static QueryContainer GetWeightedReviewQueryContainer(string term, Movie movie)
        {
            if (term is null)
            {
                throw new ArgumentNullException(nameof(term));
            }

            if (movie is null)
            {
                throw new ArgumentNullException(nameof(movie));
            }

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
                    ) && q2.Term(t => t         // to match the movie id passed in
                        .Field(f => f.MovieID)
                        .Value(movie.MovieID)
                    )
                )
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
            );
        }
    }
}