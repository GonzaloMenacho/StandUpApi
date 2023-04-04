using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace API.services
{
    public class Get10
    {
        public static async Task<ISearchResponse<Movie>> GetMovies(IElasticClient _elasticClient)
        {
            var response = await _elasticClient.SearchAsync<Movie>(s => s
            .Index(MoviesController.movieIndex)
            .Query(q => q
                .MatchAll()
                )
            );
            // returns all movies (actually defaults to first 10)
            return response;
        }

        public static async Task<ISearchResponse<Review>> GetReviews(IElasticClient _elasticClient)
        {
            var response = await _elasticClient.SearchAsync<Review>(s => s
            .Index(ReviewsController.reviewIndex)
            .Query(q => q
                .MatchAll()
                )
            );
            // returns all movies (actually defaults to first 10)
            return response;
        }
    }
}
