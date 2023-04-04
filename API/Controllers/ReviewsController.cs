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

        public static readonly string reviewIndex = "reviews";
        private readonly IElasticClient _elasticClient;

        // dictionary for fields. key is class attribute (lowercase), value is elastic field name
        public static Dictionary<string, string> ReviewFields = new Dictionary<string, string>(){
            {"dateofreview", "Date of Review"},
            {"movieid", "movieID"},
            {"reviewbody", "Review" },
            {"reviewtitle", "Review Title"},
            {"totalvotes", "Total Votes" },
            {"usefulnessvote", "Usefulness Vote" },
            {"username", "User" },
            {"userrating", "User's Rating out of 10" },
            {"reviewuserrating", "User's Rating out of 10" }
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
            try
            {
                var response = await Get10.GetReviews(_elasticClient);
                // returns all reviews (actually defaults to first 10)

                return Ok(response.Documents.ToList());
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        /// <summary>
        /// Can search for an exact match of field to search terms.
        /// </summary>
        /// <param name="field">The field to search movies by. Must match the capitalization and spelling of the elasticsearch field, not the model's attribute.</param>
        /// <param name="searchTerms">An array of all the terms you want to search for.</param>
        /// <returns></returns>
        [HttpGet("token/{field}")]
        public async Task<ActionResult<List<Review>>> GetReviewData([FromRoute] string field, [FromQuery] string[] searchTerms)
        {
            string eField = ReviewFields["reviewtitle"]; // default
            try
            {
                eField = ReviewFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            try
            {
                var response = await _elasticClient.SearchAsync<Review>(s => s
                                .Index(reviewIndex)
                                .Query(q => matchService
                                .MatchRequest(eField, new Review(), searchTerms)));
                return Ok(response.Documents.ToList());
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

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
        public async Task<ActionResult<List<Review>>> GetMinMax([FromRoute] string field, [FromQuery] float minNum, [FromQuery] float maxNum)
        {
            string eField = ReviewFields["userrating"]; // default
            try
            {
                eField = ReviewFields[field.ToLower().Trim()];
            }
            catch (Exception e) { }

            if (minNum > maxNum)
            {
                return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
            }

            try
            {
                var response = await _elasticClient.SearchAsync<Review>(s => s.Index(reviewIndex).Query(q => minMaxService.RangeRequest(eField, new Review(), minNum, maxNum)));
                return Ok(response.Documents.ToList());
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// Uses AdvancedSearchForm to accept multiple criteria for a search on Review only
        /// </summary>
        /// <param name="form">An AdvancedSearchForm object that holds all attributes which we can search. 
        /// If an attribute is left null, we don't search it. 
        /// All min-max attributes should be a numeric array of size 2. [minNum, maxNum].
        /// All strings must be in quotes</param>
        /// <returns></returns>
        [HttpPost("advSearchReviewV2")]
        public async Task<ActionResult<List<Review>>> advSearchMovieV2([FromBody] AdvancedSearchForm form)
        {
            try
            {
                var response = await _elasticClient.SearchAsync<Review>(s => s
                                .Index(reviewIndex)
                                .Query(q => dynamicAdvSearch.SingleIndexRequest(new Review(), form)
                                    )
                                );
                return Ok(response.Documents.ToList());
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}

