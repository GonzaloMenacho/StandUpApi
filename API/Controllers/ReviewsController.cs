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
    public class ReviewsController : ControllerBase
    {

        private readonly string reviewIndex = "reviews";
        private readonly IElasticClient _elasticClient;

        // create elasticClient field thru injection
        public ReviewsController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;

        }

        [HttpGet("get10")] //api/movies
        public async Task<ActionResult<List<Reviews>>> GetReviews()
        {
            var response = await _elasticClient.SearchAsync<Reviews>(s => s
                .Index(reviewIndex)
                .Query(q => q.MatchAll()));
            // returns all movies (actually defaults to first 10)

            return response.Documents.ToList();
        }
    }
}

