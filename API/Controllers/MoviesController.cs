using Microsoft.AspNetCore.Mvc;
using Nest;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
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
                .Index("movies")
                .Query(q => q.MatchAll()));
            // returns all movies

            return response.Documents.ToList();
        }

        // return movie based on the movie name path
        // pass index name here if we want to deal with different indices
        [HttpGet("{title}")] //api/movies/movieX
        public async Task<Movie> Get(string title)
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
                .Index("movies") // index name
                .Query(q => q.Term(t => t.Title, title) || q.Match(m => m.Field(f => f.Title).Query(title)))); // term allows to find docs matching an exact query
                                                                                                               // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.FirstOrDefault();
        }

        [HttpPost("add")]
        public async Task<string> Post(Movie value)
        {
            var response = await _elasticClient.IndexAsync<Movie>(value, x => x.Index("movies")); // pass incoming value and index
            return response.Id; // Id created when making a post call
        }

        [HttpDelete("del/{id}")]
        // delete based on id (http://localhost:9200/movies/_search) -> find id
        public async void Delete(string id)
        {
            var response = await _elasticClient.DeleteAsync<Movie>(id, d => d
              .Index("movies"));

        }

        [HttpPut("edit/{id}")]
        public async Task<Movie> Put(string id, string title, string rating, string description )
        {

            var response = await _elasticClient.UpdateAsync<Movie>(id, u => u
                .Index("movies")
                .Doc(new Movie { Title = title, Rating = rating, Description = description}));

            return null;
        }

    }
}