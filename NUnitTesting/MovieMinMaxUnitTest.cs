namespace NUnitTesting
{
    internal class MovieMinMaxUnitTest : AbstractTestBase
    {
        [Test]
        [TestCase(null, null, null)]
        [TestCase('movieIMDbRating', null, null)]
        [TestCase(null, 1.3, null)]
        [TestCase(null, null, 8.6)]
        [TestCase('totalRatingCount', 100000, 2000000)]


        public async Task TestMinMaxResponse(string field, float minNum, float maxNum)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromMinMax(field, minNum, maxNum);

            // Assert
            Assert.NotNull(response.Result);
            Assert.IsInstanceOf<OkObjectResult>(response.Result);

            var movies = ((OkObjectResult)response.Result).Value as List<Movie>;
            Assert.That(movies.Count, Is.EqualTo(10));
        }
    }
}