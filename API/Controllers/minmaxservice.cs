//
//TODO: this simply does not work. make a proper query builder
//
using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    public class minmaxservice : ControllerBase
    {
        private readonly string reviewIndex = "reviews";
        private IElasticClient _elasticClient;
        // regex for floating numbers 0 - 10
        // private readonly Regex floatRegex = new Regex(@"^(10(\.0+)?|[0-9](\.[0-9]+)?|\.[0-9]+)$");
        public minmaxservice(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }
        
        public ActionResult<List<Review>> GetNum(string specificNum, float minNum, float maxNum, string FieldToSearch)
        {
            // check to see if there is a specific rating
            if (!string.IsNullOrEmpty(specificNum))
            {
                // will return search request
                var response = _elasticClient.Search<Review>(s => s
                // sort by relevancy score
                .Sort(ss => ss
                    .Descending(SortSpecialField.Score)
                )
                .Index(reviewIndex) // index name
                .Query(q => q.Term(FieldToSearch, specificNum) || q.Match(m => m.Field(FieldToSearch).Query(specificNum))));
                // term allows finding docs matching an exact query
                // match allows for the user to enter in some text and that text to match any part of the content in the document
                return response.Documents.ToList();

            }

            // for range of ratings
            // TODO: get this working
            else
            {
                if (minNum > maxNum)
                {
                    return BadRequest("The 'minRating' parameter must be less than 'maxRating'");
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
                                .Field(FieldToSearch)
                                .GreaterThanOrEquals(minNum)
                                    .LessThanOrEquals(maxNum))
                            )
                        );
                return response.Documents.ToList();
            }
        }
    }
}
