using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
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
                .Query(q => q.MatchAll()));
            // returns all movies (actually defaults to first 10)

            return response.Documents.ToList();
        }

        //TODO: Pagination
        /*[HttpGet("all")] //api/movies
        public async Task<List<Movie>> GetAllMovies()
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => q).Scroll("10s"));
            // scrolls through the docs for 10 seconds)
            List<Movie> responseList = new List<Movie>();

            while (response.Documents.Any())       // keep scrolling until there are no documents
            {
                responseList.AddRange(response.Documents.ToList());
                response = _elasticClient.Scroll<Movie>("10s", response.ScrollId);
            }

            return responseList;
        }
        */

        //------------------------------------------------------------------/
        // Search for movies by rating 
        // either with a specific rating param
        // or a range rating

        // regex for floating numbers 0 - 10
        private readonly Regex ratingRegex = new Regex(@"^(10(\.0+)?|[0-9](\.[0-9]+)?|\.[0-9]+)$");

        [HttpGet("rating")] //api/movies/rating
        public async Task<ActionResult<List<Movie>>> GetRating(string specificRating, float minRating = 0, float maxRating = 10)
        {
            // check to see if there is a specific rating
            if (!string.IsNullOrEmpty(specificRating))
            {
                // check if input is a floating number 0 - 10
                if (!float.TryParse(specificRating, out float ratingValue) || !ratingRegex.IsMatch(specificRating))
                {
                    return BadRequest("The 'rating' parameter must be a number between 0 & 10.");
                }
                // will return search request
                var response = await _elasticClient.SearchAsync<Movie>(s => s
                // sort by relevancy score
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
                .Index(movieIndex) // index name
                .Query(q => q.Term(r => r.MovieIMDbRating, specificRating) || q.Match(m => m.Field(f => f.MovieIMDbRating).Query(specificRating))));
                // term allows finding docs matching an exact query
                // match allows for the user to enter in some text and that text to match any part of the content in the document
                return response.Documents.ToList();

            }

            // for range of ratings
            else
            {
                if (minRating > maxRating)
                {
                    return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
                }

                // regex check
                else if (!ratingRegex.IsMatch(minRating.ToString()) || !ratingRegex.IsMatch(maxRating.ToString()))
                {
                    return BadRequest("The 'minRating' and 'maxRating' must between 0 and 10");
                }

                // return search request
                var response = _elasticClient.Search<Movie>(s => s
                // sort by relevancy score
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
                    .Index(movieIndex)
                        .Query(q => q
                            .Range(r => r
                                .Field(f => f.MovieIMDbRating)
                                .GreaterThanOrEquals(minRating)
                                    .LessThanOrEquals(maxRating))
                            )
                        );
                return response.Documents.ToList();
            }

        }


        //------------------------------------------------------------------/
        // Search for movies by titles

        //split the title string into a list of tokens
        //use regex to process the tokens according to restrictions
        //send post-processed tokens to search function
        //return the search results

        [HttpGet("title")] //api/movies/{title}
        public async Task<ActionResult<List<Movie>>> GetMoviesByTitle(string m_title = "")
        {
            //pre-processing
            m_title = Regex.Replace(m_title, @"[^\w- ]|_", " ");   //replace illegal chars and underscores with a space
            m_title = m_title.Trim();   // trim leading and ending whitespaces

            // replacing each character in the string with regex that ignores capitalization.
            // i.e. "a" and "A" become "[Aa]"
            string fixed_title = "";
            foreach (char c in m_title)
            {
                if (c >= 'a' && c <= 'z')
                {
                    fixed_title += $"[{(char)(c - 32)}{c}]";
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    fixed_title += $"[{c}{(char)(c + 32)}]";
                }
                else
                {
                    fixed_title += $"[{c}]?";   // if " ", "-", or other special character, make it zero or 1
                }
            }
            //Console.WriteLine(fixed_title + "end");

            // regex to grab every string with the fixed_title as the substring
            m_title = $".*{fixed_title}.*";


            //searching
            var response = await _elasticClient.SearchAsync<Movie>(s => s
            // this should sort by relevancy score, but testing with optional characters in the above
            // regex creation doesn't show it working. needs to be looked into.
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
            //query the given movie index
                .Index(movieIndex)
                .Query(q => q
                    .Regexp(c => c
                        .Name(m_title)          // naming the search (not important!)
                        .Field(p => p.Title)    // the field we're querying
                        .Value(m_title)         // the query value
                        .Rewrite(MultiTermQueryRewrite.TopTerms(5)) //this limits the search to the top 5 items
                    )
                )
            );

            return response.Documents.ToList();
        }

        //------------------------------------------------------------------/
        // return movie based on its movie id
        // only returns the first 10 matches, but should only have 1 result anyway

        [HttpGet("{movieID}")] //api/movies/{movieID}
        public async Task<ActionResult<List<Movie>>> GetByID(string movieID)
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex) // index name
                .Query(q => q.Term(m => m.MovieID, movieID) || q.Match(m => m.Field(f => f.MovieID).Query(movieID))));
            // term allows finding docs matching an exact query
            // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.ToList();
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

        
        [HttpGet("multiqueryByField")]
        /// <param name="field">Must match the capitalization and spelling of the elasticsearch field, not the model's attribute</param>

        public async Task<ActionResult<List<Movie>>> GetMovieData([FromQuery] string field, [FromQuery] string[] searchTerms)
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s.Index(movieIndex).Query(q => basicMultiStringMatch.ShouldMatchRequest(field, searchTerms)));
            return response.Documents.ToList();
        }
        

        // TODO: cant use arrays with this algorithm. is it necessary?
        /*[HttpPut("edit/{elasticId}")]
        public async Task<Movie> Put(string elasticId, 
            int movieID, 
            string title, 
            float userrating, 
            double totalratingcount, 
            string totaluserreviews, 
            int totalcriticreviews, 
            int criticrating, 
            //string[] genres, 
            //string[] directors, 
            DateTime datepublished, 
            DateTime timestamp, 
            //string[] creators, 
            //string[] mainstars, 
            string description, 
            int duration, 
            string movietrailer, 
            string movieposter)
        {
            var response = await _elasticClient.UpdateAsync<Movie>(elasticId, u => u
                .Index(movieIndex)
                .Doc(new Movie { 
                    MovieID = movieID, 
                    Title = title, 
                    movieIMDbRating = userrating,
                    TotalRatingCount = totalratingcount,
                    TotalUserReviews = totaluserreviews,
                    TotalCriticReviews = totalcriticreviews,
                    MetaScore = criticrating,
                    //MovieGenres = genres, 
                    //Directors = directors, 
                    DatePublished = datepublished, 
                    Timestamp = timestamp,
                    //Creators = creators,
                    //MainStars = mainstars,
                    Description = description,
                    Duration = duration, 
                    MovieTrailer = movietrailer, 
                    MoviePoster = movieposter}));

            return null;
        }
        */
    }
}
