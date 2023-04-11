using API;
using API.Controllers;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Nest;

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
    }
}