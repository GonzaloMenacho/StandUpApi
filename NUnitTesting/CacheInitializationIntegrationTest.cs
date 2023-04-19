namespace NUnitTesting
{
    internal class CacheInitializationUnitTest : AbstractTestBase
    {
        [Test]
        public async Task TestCacheInitializationResponse()
        {
            // Act
            var response = await _movieController.GetMovieReviewForCacheInitialization();

            // Assert
            Assert.NotNull(response.Result);
            Assert.IsInstanceOf<OkObjectResult>(response.Result);




        }
        [Test]
        public async Task TestCacheInitializationContentType()
        {
            // Act
            var response = await _movieController.GetMovieReviewForCacheInitialization();

            // Assert
            Assert.NotNull(response.Result);

            Assert.IsInstanceOf<MovieReview>(((OkObjectResult)response.Result).Value);
        }

        [Test]
        public async Task TestCacheMovieIDCorrelation()
        {
            // Act
            var response = await _movieController.GetMovieReviewForCacheInitialization();

            // Assert
            // converts response to a readable movie review
            var movies = ((OkObjectResult)response.Result).Value as MovieReview;

            // each movie document correlates to their reviews
            for(int i = 0; i < movies.MovieDocuments.Count; i++){
                Assert.That(movies.MovieDocuments[i].MovieID,
                Is.EqualTo(movies.ReviewDocuments[i][0].MovieID));
            }
        }
    }
}
