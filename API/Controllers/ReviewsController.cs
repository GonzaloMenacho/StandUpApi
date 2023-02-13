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

        [HttpGet("get10")] //api/reviews
        public async Task<ActionResult<List<Review>>> GetReviews()
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex)
                .Query(q => q.MatchAll()));
            // returns all reviews (actually defaults to first 10)

            return response.Documents.ToList();
        }

        [HttpGet("getall (DO NOT CALL)")] //api/reviews   Should not call. Included just in case but dude. theres ~50k reviews
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

        /*
        [HttpGet("{title}")] //api/movies/movieX
        public async Task<Review> Get(string title)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex) // index name
                .Query(q => q.Term(t => t.ReviewTitle, title) || q.Match(m => m.Field(f => f.ReviewTitle).Query(title)))); // term allows to find docs matching an exact query
                                                                                                               // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.FirstOrDefault();
        }
        */

        //------------------------------------------------------------------/
        // Search for reviews by text content

        //split the title string into a list of tokens
        //use regex to process the tokens according to restrictions
        //send post-processed tokens to search function
        //return the search results

        [HttpGet("text/")] //api/reviews/text/{m_text}
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
        // pass index name here if we want to deal with different indices
        [HttpGet("GetByID/")] //api/reviews/GetByID/{movieID}
        public async Task<ActionResult<List<Review>>> GetByID(string movieID)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
                .Index(reviewIndex) // index name
                .Query(q => q.Term(r => r.MovieID, movieID) || q.Match(m => m.Field(f => f.MovieID).Query(movieID))));
            // term allows finding docs matching an exact query
            // match allows for the user to enter in some text that text to match any part of the content in the document

            return response.Documents.ToList();
        }

        [HttpGet("GetAllByID/")] //api/reviews/GetByID/{movieID}
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
    }
}

