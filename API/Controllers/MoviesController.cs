using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        [HttpGet] //api/movies
        public async Task<ActionResult<List<Movie>>> GetMovies()
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex)
                .Query(q => q.MatchAll()));
            // returns all movies (actually defaults to first 10)

            return response.Documents.ToList();
        }

        // return movie based on the movie name path
        // pass index name here if we want to deal with different indices
        [HttpGet("{title}")] //api/movies/movieX
        public async Task<Movie> Get(string title)
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index(movieIndex) // index name
                .Query(q => q.Term(t => t.Title, title) || q.Match(m => m.Field(f => f.Title).Query(title)))); // term allows to find docs matching an exact query
                                                                                                               // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.FirstOrDefault();
        }

        [HttpPost("add")]
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
                    } catch (Exception e) { returnString += "Failed to post movie.\n" + e;}
                }
                catch(Exception e) { returnString += "Invalid File. Make sure the file is JSON and not NDJSON\n" + e; }

            }
            return returnString;
        }

        [HttpDelete("del/{elasticId}")]
        // delete based on id (http://localhost:9200/movies/_search) -> find id
        public async void Delete(string elasticId)
        {
            var response = await _elasticClient.DeleteAsync<Movie>(elasticId, d => d
              .Index(movieIndex));

        }

        [HttpPut("edit/{elasticId}")]
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
        

        // TODO: cant use arrays with this algorithm. figure it out
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

class JsonHandler
{
    public static string ReadAsList(IFormFile file)
    {
        var result = new StringBuilder();
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            while (reader.Peek() >= 0)
                result.AppendLine(reader.ReadLine());
        }
        return result.ToString();
    }
}

