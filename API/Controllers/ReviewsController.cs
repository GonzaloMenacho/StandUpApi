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


        //------------------------------------------------------------------/
        // Returns first 10 reviews

        [HttpGet("get10")] //api/reviews
        public async Task<ActionResult<List<Review>>> GetReviews()
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex)
                .Query(q => q.MatchAll()));
            // returns all reviews (actually defaults to first 10)

            return response.Documents.ToList();
        }


        //------------------------------------------------------------------/
        // Returns all reviews
        // TODO: Implement pagination
        // Should not call until pagination is implemented.
        // Included just in case but dude. theres ~50k reviews

        [HttpGet("getall (DO NOT CALL)")] //api/reviews   
        public async Task<List<Review>> GetAllReviews()
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex)
                .Query(q => q).Scroll("10s"));
            // scrolls through the docs for 10 seconds)
            List<Review> responseList = new List<Review>();

            while (response.Documents.Any())       // keep scrolling until there are no documents
            {
                responseList.AddRange(response.Documents.ToList());
                response = _elasticClient.Scroll<Review>("10s", response.ScrollId);
            }

            return responseList;
        }


        //------------------------------------------------------------------/
        // Search for reviews by username

        [HttpGet("GetByUser")] //api/movies/GetByUser
        public async Task<ActionResult<List<Review>>> Get(string username)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex) // index name
                .Query(q => q.Term(t => t.Username, username) || q.Match(m => m.Field(f => f.Username).Query(username)))); // term allows to find docs matching an exact query
                                                                                                               // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.ToList();
        }


        //------------------------------------------------------------------/
        // Search for reviews by text content

        //split the title string into a list of tokens
        //use regex to process the tokens according to restrictions
        //send post-processed tokens to search function
        //return the search results

        [HttpGet("GetByBody")] //api/reviews/GetByBody/{m_text}
        public async Task<ActionResult<List<Review>>> GetReviewsByText(string m_text = "", bool searchOnTitle = true)
        {
            //pre-processing
            string originaltext = m_text;
            m_text = Regex.Replace(m_text, @"[^\w- ]|_", " ");   //replace illegal chars and underscores with a space
            m_text = m_text.Trim();   // trim leading and ending whitespaces

            // replacing each character in the string with regex that ignores capitalization.
            // i.e. "a" and "A" become "[Aa]"
            string fixed_text = "";
            
            foreach (char c in m_text)
            {
                if (c >= 'a' && c <= 'z')
                {
                    fixed_text += $"[{(char)(c - 32)}{c}]";
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    fixed_text += $"[{c}{(char)(c + 32)}]";
                }
                else
                {
                    fixed_text += $"[{c}]?";   // if " ", "-", or other special character, make it zero or 1
                }
            }
            //Console.WriteLine(fixed_text + "end");

            // regex to grab every string with the fixed_title as the substring
            m_text = $".*{fixed_text}.*";
            

            //searching
            if (searchOnTitle)
            {
                var response = await _elasticClient.SearchAsync<Review>(s => s
                    // this should sort by relevancy score, but testing with optional characters in the above
                    // regex creation doesn't show it working. needs to be looked into.
                    .Sort(ss => ss
                        .Descending(SortSpecialField.Score)
                    )
                    //query the given review index
                    .Index(reviewIndex)
                    .Query(q => q
                        .Regexp(c => c
                            .Name(m_text)          // naming the search (not important!)
                            .Field(p => p.ReviewTitle)    // the field we're querying
                            .Value(m_text)         // the query value
                            .Rewrite(MultiTermQueryRewrite.TopTerms(5)) //this limits the search to the top 5 items
                        ) || q.Term(t => t.ReviewTitle, originaltext) 
                        || q.Match(m => m.Field(f => f.ReviewTitle).Query(originaltext))
                    )
                );
                return response.Documents.ToList();
            }
            else
            {
                var response = await _elasticClient.SearchAsync<Review>(s => s
                    // this should sort by relevancy score, but testing with optional characters in the above
                    // regex creation doesn't show it working. needs to be looked into.
                    .Sort(ss => ss
                        .Descending(SortSpecialField.Score)
                    )
                    //query the given review index
                    .Index(reviewIndex)
                    .Query(q => q
                        .Regexp(c => c
                            .Name(m_text)          // naming the search (not important!)
                            .Field(p => p.ReviewBody)    // the field we're querying
                            .Value(m_text)         // the query value
                            .Rewrite(MultiTermQueryRewrite.TopTerms(5)) //this limits the search to the top 5 items
                        ) || q.Term(t => t.ReviewBody, originaltext) 
                        || q.Match(m => m.Field(f => f.ReviewBody).Query(originaltext))
                    )
                );
                return response.Documents.ToList();
            }
        }


        //------------------------------------------------------------------/
        // return review based on the movie its reviewing
        // only returns the first 10 matches

        [HttpGet("GetByID")] //api/reviews/GetByID/{movieID}
        public async Task<ActionResult<List<Review>>> GetByID(string movieID)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex) // index name
                .Query(q => q.Term(r => r.MovieID, movieID) || q.Match(m => m.Field(f => f.MovieID).Query(movieID))));
            // term allows finding docs matching an exact query
            // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.ToList();
        }

        //------------------------------------------------------------------/
        // return review based on the movie its reviewing
        // returns ALL results. Big load, will be slow
        // TODO: implement pagination

        [HttpGet("GetAllByID")] //api/reviews/GetByID/{movieID}
        public async Task<ActionResult<List<Review>>> GetAllByID(string movieID)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex) // index name
                .Query(q => q.Term(r => r.MovieID, movieID) ||          // term allows finding docs matching an exact query
                q.Match(m => m.Field(f => f.MovieID).Query(movieID)))   // match allows for the user to enter in some text that text to match any part of the content in the document
                .Scroll("10s"));                                        // scrolls through the docs for 10 seconds)

            List<Review> responseList = new List<Review>();

            while (response.Documents.Any())       // keep scrolling until there are no documents
            {
                responseList.AddRange(response.Documents.ToList());
                response = _elasticClient.Scroll<Review>("10s", response.ScrollId);
            }

            return responseList;            // long response time. TODO: implement pagination??? do something to chunk out the responses.
        }

        //------------------------------------------------------------------/
        // Search for reviews by rating 
        // either with a specific rating param
        // or a range rating
        // Gonzalo's code copy pasted from MovieController
        // TODO: Create an index toggle for this function and other copy pasted request?

        // regex for floating numbers 0 - 10
        private readonly Regex ratingRegex = new Regex(@"^(10(\.0+)?|[0-9](\.[0-9]+)?|\.[0-9]+)$");

        [HttpGet("rating/")] //api/reviews/rating/{rating}
        public async Task<ActionResult<List<Review>>> GetRating(string specificRating, float minRating = 0, float maxRating = 10)
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
                var response = await _elasticClient.SearchAsync<Review>(s => s
                // sort by relevancy score
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
                .Index(reviewIndex) // index name
                .Query(q => q.Term(r => r.UserRating, specificRating) || q.Match(m => m.Field(f => f.UserRating).Query(specificRating))));
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
                var response = _elasticClient.Search<Review>(s => s
                // sort by relevancy score
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
                    .Index(reviewIndex)
                        .Query(q => q
                            .Range(r => r
                                .Field(f => f.UserRating)
                                .GreaterThanOrEquals(minRating)
                                    .LessThanOrEquals(maxRating))
                            )
                        );
                return response.Documents.ToList();
            }

        }
    }
}

