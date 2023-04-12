using API.Controllers;
using API.services;
using Microsoft.AspNetCore.Mvc;
using NuGet.Frameworks;

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
            if (movies.ReviewDocuments.Count() > 0) {
                Assert.That(movies.MovieDocuments.Count(), Is.GreaterThan(0));
            }

            // If we matched on a movie title, assert that we are getting reviews back.
            if (movies.MovieDocuments.Count() < 10)
            {
                Assert.That(movies.MovieDocuments.Count(), Is.EqualTo(movies.ReviewDocuments.Count()));
            }
            
        }

        [Test]
        [TestCase("")]
        [TestCase("avengers")]
        [TestCase("aven")]
        [TestCase("good movie")]
        [TestCase("asovuboaiwba09")]
        [TestCase("{}!@sdoih';';......-=-=:::")]
        [TestCase(")!@*)&%)&@Morbius)!&@)&(#@&#%(")]
        public async Task TestBasicSearchContentType(string? term)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromTerm(term);

            // Assert
            Assert.IsInstanceOf<MovieReview>(((OkObjectResult)response.Result).Value);

        }


        [Test]
        [TestCase("")]
        [TestCase("avengers")]
        [TestCase("aven")]
        [TestCase("good movie")]
        [TestCase("asovuboaiwba09")]
        [TestCase("{}!@sdoih';';......-=-=:::")]
        [TestCase(")!@*)&%)&@Morbius)!&@)&(#@&#%(")]
        public async Task TestBasicSearchMovieIDCorrelation(string? term)
        {
            // Act
            var response = await _movieController.GetMovieReviewFromTerm(term);

            // Assert
            var movies = ((OkObjectResult)response.Result).Value as MovieReview;
            for (int i = 0; i < movies.MovieDocuments.Count(); i++)
            {
                Assert.That(movies.MovieDocuments[i].MovieID,
                    Is.EqualTo(movies.ReviewDocuments[i][0].MovieID));
            }
        }
    }
}
