using API;
using API.Controllers;
using API.services;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Net;

namespace NUnitTesting
{
    [TestFixture]
    public class Tests
    {
        private IElasticClient _elasticClient;
        private MoviesController _movieController;

        [SetUp]
        public void Setup()
        {
            //var config = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            _elasticClient = new ElasticClient();
            _movieController = new MoviesController(_elasticClient);
        }
        
        [Test]
        public async Task Get10MoviesTest()
        {
            // Act
            var response = await _movieController.GetMovies();

            // Assert
            Assert.NotNull(response.Result);
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            var movies = ((OkObjectResult)response.Result).Value as List<Movie>;
            Assert.That(movies.Count, Is.EqualTo(10));
        }

        [Test]
        [TestCase("")]
        [TestCase("avengers")]
        [TestCase("aven")]
        [TestCase("good movie")]
        [TestCase("asovuboaiwba09")]
        [TestCase("{}!@sdoih';';......-=-=:::")]
        [TestCase(")!@*)&%)&@Morbius)!&@)&(#@&#%(")]
        public async Task BasicSearchTest(string? term)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromTerm(term);

            // Assert

            // Assert we have a response.
            Assert.NotNull(response.Result);

            // Assert Status Code 200 is returned.
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            // Assert that we have MovieDocuments always being returned;
            var movies = ((OkObjectResult)response.Result).Value as MovieReview;
            Assert.That(movies.MovieDocuments.Count(), Is.GreaterThan(0));

            // If we matched on a movie title, assert that we are getting reviews back.
            if(movies.MovieDocuments.Count() < 10)
            {
                Assert.That(movies.MovieDocuments.Count(), Is.EqualTo(movies.ReviewDocuments.Count()));
            }
        }


        [Test]
        [TestCase(null, null, null, null, null, null, null, null)]
        [TestCase("avengers", null, null, null, null, null, null, null)]
        [TestCase(null, new[] { 6.0f, 10.0f }, null, null, null, null, null, null)]
        [TestCase(null, null, new[] { 500.0f, 100000.0f }, null, null, null, null, null)]
        [TestCase(null, null, null, "awesome", null, null, null, null)]
        [TestCase(null, null, null, null, "iron", null, null, null)]
        [TestCase(null, null, null, null, null, new[] { 5.0f, 150.0f }, null, null)]
        [TestCase(null, null, null, null, null, null, new[] { 70.0f, 200.0f }, null)]
        [TestCase(null, null, null, null, null, null, null, new[] { 6.0f, 10.0f })]
        [TestCase("Thor", new[] { 5.0f, 8.0f }, null, null, null, null, null, null)]
        [TestCase("morbius", null, null, "good movie", "good movie", null, null, null)]
        [TestCase("morbius", new[] { 5.0f, 8.0f }, null, "good movie", "good movie", null, null, null)]
        [TestCase(null, null, null, "great", "bad thing", null, null, null)]
        [TestCase(null, null, null, "bad", null, null, new[] { 5.0f, 150.0f }, null)]
        [TestCase("Joker", null, null, "awesome", null, null, new[] { 55.0f, 300.0f }, null)]
        [TestCase("Aven", null, null, "pog", "great", null, new[] { 5.0f, 100.0f }, new[] { 5.0f, 10.0f})]
        public async Task AdvancedSearchTest(
            string title,
            float[] movieUserRating,
            float[] movieTotalRating,
            string reviewBody,
            string reviewTitle,
            float[] reviewTotalVotes,
            float[] reviewUsefulness,
            float[] reviewUserRating
            )
        {
            // Arrange
            var searchForm = new AdvancedSearchForm
            {
                MovieTitle = title,
                TotalUserRatingMinMax = movieUserRating,
                TotalRatingCountMinMax = movieTotalRating,
                ReviewBody = reviewBody,
                ReviewTitle = reviewTitle,
                TotalVotesMinMax = reviewTotalVotes,
                UsefulnessVoteMinMax = reviewUsefulness,
                ReviewUserRatingMinMax = reviewUserRating
            };

            // Act
            var response = await _movieController.advSearch(searchForm);

            // Assert

            // Assert we have a response.
            Assert.NotNull(response.Result);

            // Assert Status Code 200 is returned.
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            // Assert that we have MovieDocuments always being returned;
            var movies = ((OkObjectResult)response.Result).Value as MovieReview;
            Assert.That(movies.MovieDocuments.Count(), Is.GreaterThan(0));

            // If we matched on a movie title, assert that we are getting reviews back.
            if (movies.MovieDocuments.Count() < 10)
            {
                Assert.That(movies.MovieDocuments.Count(), Is.EqualTo(movies.ReviewDocuments.Count()));
            }
        }
    }
}