namespace NUnitTesting
{
    internal class Get10MoviesUnitTest : AbstractTestBase
    {
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
    }
}
