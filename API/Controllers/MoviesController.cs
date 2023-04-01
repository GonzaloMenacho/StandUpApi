using API.services;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {

        private readonly string movieIndex = "movies";
        private readonly IElasticClient _elasticClient;

        // dictionary for fields. key is class attribute (lowercase), value is elastic field name
        private Dictionary<string, string> MovieFields = new Dictionary<string, string>(){
            {"movieid", "movieID"},
            {"title", "title"},
            {"movieimdbrating", "movieIMDbRating" },
            {"totalratingcount", "totalRatingCount"},
            {"totaluserreviews", "totalUserReviews" },
            {"totalcriticreviews", "totalCriticReviews" },
            {"metascore", "metaScore" },
            {"moviegenres", "movieGenres" },
            {"directors", "directors" },
            {"datepublished", "datePublished" },
            {"timestamp", "@timestamp" },
            {"creators", "creators" },
            {"mainstars", "mainStars" },
            {"description", "description"},
            {"duration", "duration" },
            {"movietrailer", "movieTrailer" },
            {"moviesposter", "moviePoster" }
        };

        // create elasticClient field thru injection
        public MoviesController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;

        }

        [HttpGet("")] //api/movies
        public async Task<ActionResult<List<Movie>>> GetMovies()
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => q
                    .MatchAll()
                    )
                );
            // returns all movies (actually defaults to first 10)

            return response.Documents.ToList();
        }

        /// <summary>
        /// Can search for an exact match of field to search terms. Matches on by characters using regex. Monk matches monkey.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("char/{field}")]
        public async Task<ActionResult<List<Movie>>> GetMovieDataByChar([FromRoute] string field, [FromQuery] string[] searchTerms)
        {
            string eField = "title"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => searchByCharRaw
                    .RegexpRequest(eField, movieOBJ, searchTerms)
                    )
                );
            return response.Documents.ToList();
        }

        /// <summary>
        /// Can search for an exact match of field to search terms. Matches on a word-by-word basis. Monkey matches monkey, but Monk does not match monkey.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("token/{field}")]
        public async Task<ActionResult<List<Movie>>> GetMovieDataByToken([FromRoute] string field, [FromQuery] string[] searchTerms)
        {
            string eField = "title"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => matchService
                    .MatchRequest(eField, movieOBJ, searchTerms)
                    )
                );
            return response.Documents.ToList();
        }

        /// <summary>
        ///  Can read any review field and find all reviews that match a specific number OR fit within a passed range on the chosen field.
        /// </summary>
        /// <param name="field">The field within a review to search on.</param>
        /// <param name="specificNum">The exact number to match on the field</param>
        /// <param name="minNum">The lower bound on the field (inclusive)</param>
        /// <param name="maxNum">The higher bound on the field (inclusive)</param>
        /// <returns></returns>
        [HttpGet("minmax/{field}")] //api/reviews/minmaxByField
        public async Task<ActionResult<List<Movie>>> GetMinMax([FromRoute] string field, [FromQuery] string specificNum, [FromQuery] float minNum, [FromQuery] float maxNum)
        {
            string eField = "metaScore"; // default
            try
            {
                eField = MovieFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Movie movieOBJ = new Movie();
            if (!string.IsNullOrEmpty(specificNum))
            {

                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index(movieIndex)
                    .Query(q => matchService
                        .MatchRequest(eField, movieOBJ, specificNum)
                        )
                    );
                return response.Documents.ToList();
            }
            else
            {
                if (minNum > maxNum)
                {
                    return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
                }

                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index(movieIndex)
                    .Query(q => minMaxService
                        .RangeRequest(eField, movieOBJ, minNum, maxNum)
                        )
                    );
                return response.Documents.ToList();
            }
        }

        /// <summary>
        /// For each object, it will add a query on that object's field, searching on the search terms belonging to that object. It will add all queries to one request.
        /// Each hit will abide by all queries from all objects passed (all search terms on each respective field).
        /// </summary>
        /// <param name="fieldTerms">A list of FieldTerms objects. Each object has a string "field" and an array of strings "search terms." </param>
        /// <returns></returns>
        [HttpPost("advancedSearchProto")]
        public async Task<ActionResult<List<Movie>>> ByTokenPerField([FromBody] List<FieldTerms> fieldTerms)
        {
            int iter = 0;
            List<int> badQueries = new List<int>();
            string eField = "title"; // default

            //FieldTerms test = new FieldTerms();
            //test.searchTerms = new string[] { "Morbius" };
            //test.field = "title";

            //fieldTerms.Add(test);

            foreach (var query in fieldTerms)
            {
                try
                {
                    eField = MovieFields[query.field.ToLower().Trim()];
                    query.field = eField;
                }
                catch (Exception e)
                {
                    badQueries.Add(iter);
                }
                iter++;
            }

            foreach (int index in badQueries)
            {
                fieldTerms.RemoveAt(index);
            }

            Movie movieOBJ = new Movie();
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => multiFieldMatch
                    .MatchRequest(movieOBJ, fieldTerms)
                    )
                );
            return response.Documents.ToList();
        }


        [HttpGet("search")]
        public async Task<ActionResult<MovieReview>> GetMovieReviewFromTerm(string? term = null)
        {
            try
            {
                var movieReview = await BasicSearchService.GetMovieReviewFromTerm(_elasticClient, term);
                return Ok(movieReview.Value);   // return the MR object with status 200
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        [HttpPost("cooler-advanced-search")]
        public async Task<ActionResult<MovieReview>> GetMovieReviewFromTerm([FromQuery] string[]? title,
                                                                            [FromQuery] string[]? keyword,
                                                                            [FromQuery] string[]? rating,
                                                                            [FromQuery] string[]? genre,
                                                                            [FromQuery] string[]? reviewscore)
        {
            // search movies //
            List<Movie> movielist = new List<Movie>();
            try
            {
                Movie movieobj = new Movie();
                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index(movieIndex)
                    .Query(q =>
                        searchByCharRaw.RegexpRequest("title", movieobj, title)
                        )
                    );
                if (!response.IsValid)   // if we didn't get the review json back
                {
                    return BadRequest("Failed to retrieve movie jsons in cooler-advanced-search.");
                }
                movielist = response.Documents.ToList(); // append the response to the movie list.
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

            if (movielist.Count == 0)
            {
                // get all the movie documents in the db
                // returns 10 movies
                try
                {
                    var res = await _elasticClient.SearchAsync<Movie>(s => s
                        .Index(movieIndex)
                        .Size(10)
                        .Query(q => q
                            .MatchAll()
                            )
                        );
                    if (!res.IsValid)  // if we didn't reach the database for the movies
                    {
                        return BadRequest("Failed to find movies in GetMovieReviewFromTerm.");
                    }
                    movielist = res.Documents.ToList();
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
                }
            }


            // search reviews //
            /*
             * [FromQuery] string[]? title,
             * [FromQuery] string[]? keyword,
             * [FromQuery] string[]? rating,
             * [FromQuery] string[]? genre,
             * [FromQuery] string[]? reviewscore
             * 
             */
            List<List<Review>> reviewlist = new List<List<Review>>();
            foreach (Movie movie in movielist)
            {
                // convert the reviewscore strings to doubles and default it to a range of 0 to 10
                double[] reviewscoreDoubles = reviewscore.Select(x => double.TryParse(x, out double result) ? result : 0).ToArray();
                double minScore = reviewscoreDoubles.Length > 0 ? reviewscoreDoubles[0] : 0;
                double maxScore = reviewscoreDoubles.Length > 1 ? reviewscoreDoubles[1] : 10;

                try
                {
                    var res = await _elasticClient.SearchAsync<Review>(s => s
                        .Index("reviews")
                        .Query(q => q
                            .Bool(b => b
                                .Should(m => m
                                    .MultiMatch(mm => mm
                                        .Fields(f => f
                                            .Field(ff => ff.ReviewBody)
                                            .Field(ff => ff.ReviewTitle)
                                            )
                                        .Type(TextQueryType.MostFields)
                                        .Query(keyword.Length < 1 ? "" : keyword[0])
                                        /*
                                        .Query(string.Join(" ", keyword.Where(
                                            x => !string.IsNullOrEmpty(x))))       
                                        */
                                        .Slop(2)                    // character misplacements (typos: avengers=>aevngers)
                                        .Fuzziness(Fuzziness.Auto)  // token distance (tomato soup=>soup tomatos)
                                        .PrefixLength(2)            // allows prefixes (
                                        .MaxExpansions(1)          // limits the amount of docs returned
                                        .Operator(Operator.Or)
                                        .AutoGenerateSynonymsPhraseQuery(false)
                                        ) && q      // this lets us grab movies based on movieID
                                        .Term(t => t
                                            .Field(f => f.MovieID)
                                            .Value(movie.MovieID)
                                        )
                                        ) // m=>m
                                    .Filter(f => f
                                        .Range(r => r
                                            .Field(f => f.UserRating)
                                            .GreaterThanOrEquals(minScore)
                                            .LessThanOrEquals(maxScore)
                                            )
                                  )
                              )
                            )
                        );

                    if (!res.IsValid)   // if we didn't get the review json back
                    {
                        return BadRequest("Failed to retrieve review jsons in cooler-advanced-search.");
                    }
                    reviewlist.Add(res.Documents.ToList()); // append the response to the movie list.
                }
                catch (Exception e)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
                }
            }
            

            MovieReview moviereviewlist = new MovieReview
            {
                MovieDocuments = movielist,
                ReviewDocuments = reviewlist
            };
            return moviereviewlist;
        }


        // This method should be called at the startup of the website for the first time.
        // It returns the first 10 movies in the Movie index,
        // along with 3 reviews for each movie from the Reviews index
        // this is stored in the form of the "MovieReviewInitialization" object, see MovieReviewInitialization.cs
        // plz don't delete, elijah T^T
        // LOL^
        [HttpGet("initialize-cache")]
        public async Task<ActionResult<MovieReview>> GetCacheInitialization()
        {
            try
            {
                // get all the movie documents in the db
                // returns 10 movies
                var response = await _elasticClient.SearchAsync<Movie>(s => s
                    .Index(movieIndex)
                    .Size(10)
                    .Query(q => q.MatchAll()));
                if (!response.IsValid)  // if we didn't reach the database for the movies
                {
                    return BadRequest("Failed to retrieve movie jsons in CacheInitialization.");
                }
                List<Movie> movielist = response.Documents.ToList();   // save the response for now

                List<List<Review>> reviewlist = new List<List<Review>>();
                foreach (Movie movie in movielist) // for each movie in our movie list
                {
                    int movieid = movie.MovieID;    // grab the movieID
                    var res = await _elasticClient.SearchAsync<Review>(s => s
                        .Index("reviews")   // https://tenor.com/view/now-whos-gonna-stop-me-bowser-the-super-mario-bros-movie-try-to-stop-me-now-im-unstoppable-gif-26913745
                        .Size(3)            // limits the number of hits to 3
                        .Query(q => q
                            .Match(m => m                   // perform a match query
                                .Field(f => f.MovieID)      // match on the "MovieID" field
                                .Query(movieid.ToString())  // pass the movieid as a string
                                )
                            )
                        );
                    if (!res.IsValid)   // if we didn't get the review json back
                    {
                        return BadRequest("Failed to retrieve review jsons in CacheInitialization.");
                    }
                    reviewlist.Add(res.Documents.ToList()); // append the response to the movie list.
                }

                MovieReview movieReviewInitialization = new MovieReview
                {
                    MovieDocuments = movielist,
                    ReviewDocuments = reviewlist
                };
                return Ok(movieReviewInitialization);   // return the MRI object with status 200

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        [HttpPost("")]
        public async Task<string> Post(Movie value)
        {
            // TODO: create an autoincrementing function for movieID and ensure no two movies have the same ID
            // Date automatically filled out
            var response = await _elasticClient.IndexAsync<Movie>(value, x => x.Index(movieIndex)); // pass incoming value and index
            return response.Id; // Id created when making a post call
        }

        [HttpPost("json-upload")]
        public async Task<string> Upload(List<IFormFile> files)
        {
            string returnString = "";
            // validate the json, parse it, store each movie
            foreach (IFormFile file in files)
            {
                try
                {
                    var serializerSettings = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    string json = JsonHandler.ReadAsList(file);
                    var movies = JsonSerializer.Deserialize<List<Movie>>(json, serializerSettings);
                    try
                    {
                        foreach (Movie movie in movies)
                        {
                            movie.Timestamp = movie.DatePublished;
                            string response = await Post(movie);
                            returnString += response + '\n';
                        }
                    }
                    catch (Exception e) { returnString += "Failed to post movie.\n" + e; }
                }
                catch (Exception e) { returnString += "Invalid File. Make sure the file is JSON and not NDJSON\n" + e; }

            }
            return returnString;
        }

        [HttpDelete("{elasticId}")]
        // delete based on id (http://localhost:9200/movies/_search) -> find id
        public async void Delete(string elasticId)
        {
            var response = await _elasticClient.DeleteAsync<Movie>(elasticId, d => d
              .Index(movieIndex));

        }

        [HttpPut("{elasticId}")]
        public async Task<string> Put(string elasticId, Movie value)
        {
            // TODO: create an autoincrementing function for movieID and ensure no two movies have the same ID
            // Date automatically filled out
            var response = await _elasticClient.UpdateAsync<Movie>(elasticId, u => u
                .Index(movieIndex)
                .Doc(new Movie
                {
                    MovieID = value.MovieID,
                    Title = value.Title,
                    MovieIMDbRating = value.MovieIMDbRating,
                    TotalRatingCount = value.TotalRatingCount,
                    TotalUserReviews = value.TotalUserReviews,
                    TotalCriticReviews = value.TotalCriticReviews,
                    MetaScore = value.MetaScore,
                    MovieGenres = value.MainStars,
                    Directors = value.Directors,
                    DatePublished = value.DatePublished,
                    Timestamp = value.Timestamp,
                    Creators = value.Creators,
                    MainStars = value.MainStars,
                    Description = value.Description,
                    Duration = value.Duration,
                    MovieTrailer = value.MovieTrailer,
                    MoviePoster = value.MoviePoster
                }));

            return response.Id;
        }

    }
}
