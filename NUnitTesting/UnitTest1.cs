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
            //Initialize
            var response = await _movieController.GetMovies();

            //Assert
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
        public async Task SearchTest(string? term)
        {
            var response = await _movieController.GetMovieReviewFromTerm(term);

            //Assert
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

    }
}