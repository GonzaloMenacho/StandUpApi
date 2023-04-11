using API.Controllers;
using API.services;
using Microsoft.AspNetCore.Mvc;
namespace NUnitTesting
{
    [TestFixture]
    internal class BasicSearchUnitTest : AbstractTestBase
    {
        [Test]
        [TestCase("")]
        [TestCase("avengers")]
        [TestCase("aven")]
        [TestCase("good movie")]
        [TestCase("asovuboaiwba09")]
        [TestCase("{}!@sdoih';';......-=-=:::")]
        [TestCase(")!@*)&%)&@Morbius)!&@)&(#@&#%(")]
        public async Task TestBasicSearchResponse(string? term)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromTerm(term);

            // Assert

            // Assert we have a response.
            Assert.NotNull(response.Result);

            // Assert Status Code 200 is returned.
            Assert.IsInstanceOf<OkObjectResult>(response.Result);
        }


        [Test]
        [TestCase("")]
        [TestCase("avengers")]
        [TestCase("aven")]
        [TestCase("good movie")]
        [TestCase("asovuboaiwba09")]
        [TestCase("{}!@sdoih';';......-=-=:::")]
        [TestCase(")!@*)&%)&@Morbius)!&@)&(#@&#%(")]
        public async Task TestBasicSearchDocumentCount(string? term)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromTerm(term);

            // Assert
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
