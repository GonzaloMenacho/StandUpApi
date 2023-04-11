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
    public class AbstractTestBase
    {
        private IElasticClient _elasticClient;
        protected MoviesController _movieController;

        [SetUp]
        public void Setup()
        {
            //var config = new ConnectionConfiguration(new Uri("http://localhost:9200"));
            _elasticClient = new ElasticClient();
            _movieController = new MoviesController(_elasticClient);
        }
        
        /*
        [Test]
        public async Task Get10MoviesTest()
        {
            // Act
            var response = await _movieController.GetMovieList();

            // Assert
            Assert.NotNull(response.Result);
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            var movies = ((OkObjectResult)response.Result).Value as List<Movie>;
            Assert.That(movies.Count, Is.EqualTo(10));
        }
        */
    }
}