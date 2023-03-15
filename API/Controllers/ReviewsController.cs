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
    public class ReviewsController : ControllerBase
    {

        private readonly string reviewIndex = "reviews";
        private readonly IElasticClient _elasticClient;

        // dictionary for fields. key is class attribute (lowercase), value is elastic field name
        private Dictionary<string, string> ReviewFields = new Dictionary<string, string>(){
            {"dateofreviews", "Date of Review"},
            {"movieid", "movieID"},
            {"reviewbody", "Review" },
            {"reviewtitle", "Review Title"},
            {"totalvotes", "Total Votes" },
            {"usefulnessvote", "Usefulness Vote" },
            {"username", "User" },
            {"userrating", "User's Rating out of 10" },
        };

        // create elasticClient field thru injection
        public ReviewsController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;

        }

        //------------------------------------------------------------------/
        // Returns first 10 reviews

        [HttpGet("")] //api/reviews
        public async Task<ActionResult<List<Review>>> GetReviews()
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex)
                .Query(q => q.MatchAll()));
            // returns all reviews (actually defaults to first 10)

            return response.Documents.ToList();
        }
        

        /// <summary>
        /// Can search for an exact match of field to search terms.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("multiqueryByField")]
        public async Task<ActionResult<List<Review>>> GetMovieData([FromQuery] string field, [FromQuery] string[] searchTerms)
        {
            string eField = ReviewFields["reviewtitle"]; // default
            try
            {
                eField = ReviewFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Review reviewOBJ = new Review();
            var response = await _elasticClient.SearchAsync<Review>(s => s.Index(reviewIndex).Query(q => matchService.MatchRequest(eField, reviewOBJ, searchTerms)));
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
        [HttpGet("minmaxByField")] //api/reviews/minmaxByField
        public async Task<ActionResult<List<Review>>> GetMinMax([FromQuery] string field, [FromQuery] string specificNum, [FromQuery] float minNum, [FromQuery] float maxNum) 
        {
            string eField = ReviewFields["userrating"]; // default
            try
            {
                eField = ReviewFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            Review reviewOBJ = new Review();
            if (!string.IsNullOrEmpty(specificNum))
            {
                
                var response = await _elasticClient.SearchAsync<Review>(s => s.Index(reviewIndex).Query(q => matchService.MatchRequest(eField, reviewOBJ, specificNum)));
                return response.Documents.ToList();
            }
            else
            {
                if (minNum > maxNum)
                {
                    return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
                }

                var response = await _elasticClient.SearchAsync<Review>(s => s.Index(reviewIndex).Query(q => minMaxService.RangeRequest(eField, reviewOBJ, minNum, maxNum)));
                return response.Documents.ToList();
            }
        }
    }
}

