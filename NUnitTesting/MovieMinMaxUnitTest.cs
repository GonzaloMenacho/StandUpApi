namespace NUnitTesting
{
    internal class MovieMinMaxUnitTest : AbstractTestBase
    {
        [Test]
        [TestCase("movieIMDbRating", 1.3f, 8.6f)]
        [TestCase("totalRatingCount", 100000f, 2000000f)]


        public async Task TestMinMaxResponse(string field, float minNum, float maxNum)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromMinMax(field, minNum, maxNum);

            // Assert
            Assert.NotNull(response.Result);
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            //var movies = ((OkObjectResult)response.Result).Value as List<Movie>;
           // Assert.That(movies.Count, Is.EqualTo(10));
        }

        [Test]
        [TestCase("movieIMDbRating", 1.3f, 8.6f)]
        [TestCase("totalRatingCount", 100000f, 2000000f)]
        public async Task TestMinMaxContetnt(string field, float minNum, float maxNum){
            // Act
            var response = await _movieController.GetMovieReviewFromMinMax(field, minNum, maxNum);

            // Assert
            Assert.NotNull(response.Result);

            Assert.IsInstanceOf<List<Movie>>(((OkObjectResult)response.Result).Value);
        }
    }
}
