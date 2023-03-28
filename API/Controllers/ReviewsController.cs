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
        [HttpGet("token/{field}")]
        public async Task<ActionResult<List<Review>>> GetReviewData([FromRoute] string field, [FromQuery] string[] searchTerms)
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
        [HttpGet("minmax/{field}")] //api/reviews/minmaxByField
        public async Task<ActionResult<List<Review>>> GetMinMax([FromQuery] string field, [FromRoute] string specificNum, [FromQuery] float minNum, [FromQuery] float maxNum) 
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

        /// <summary>
        /// For each object, it will add a query on that object's field, searching on the search terms belonging to that object. It will add all queries to one request.
        /// Each hit will abide by all queries from all objects passed (all search terms on each respective field).
        /// </summary>
        /// <param name="fieldTerms">A list of FieldTerms objects. Each object has a string "field" and an array of strings "search terms." </param>
        /// <returns></returns>
        [HttpPost("advancedSearchProto")]
        public async Task<ActionResult<List<Review>>> advancedSearch([FromBody] List<FieldTerms> fieldTerms)
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
                    eField = ReviewFields[query.field.ToLower().Trim()];
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

            Review reviewOBJ = new Review();
            var response = await _elasticClient.SearchAsync<Review>(s => s.Index(reviewIndex).Query(q => multiFieldMatch.MatchRequest(reviewOBJ, fieldTerms)));
            return response.Documents.ToList();
        }
    }
}

