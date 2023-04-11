namespace NUnitTesting
{
    [TestFixture]
    internal class AdvancedSearchUnitTest : AbstractTestBase
    {
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
        [TestCase("Aven", null, null, "pog", "great", null, new[] { 5.0f, 100.0f }, new[] { 5.0f, 10.0f })]
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
            var response = await _movieController.GetMovieReviewFromSearchForm(searchForm);

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
        [TestCase("Aven", null, null, "pog", "great", null, new[] { 5.0f, 100.0f }, new[] { 5.0f, 10.0f })]
        public async Task TestAdvancedSearchMovieIDCorrelation(
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
            // Makes sure each movie has reviews with a movieID on each that matches the movie

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
            var response = await _movieController.GetMovieReviewFromSearchForm(searchForm);

            var results = ((OkObjectResult)response.Result).Value as MovieReview;
            var movies = results.MovieDocuments;

            for (int i = 0; i < movies.Count(); i++)
            {
                Movie movieResult = movies[i];
                //Assert that each review has a matching movie ID
                var reviews = results.ReviewDocuments[i];
                foreach (Review reviewResult in reviews)
                {
                    Assert.That(movieResult.MovieID, Is.EqualTo(reviewResult.MovieID));
                }
            }
        }

        [TestCase(null, null, null, "awesome", null, null, null, null)]
        [TestCase(null, null, null, null, "iron", null, null, null)]
        [TestCase(null, null, null, "awesome", "iron", null, null, null)]
        public async Task TestAdvancedSearchReviewKeywordsInResults(
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
            // Makes sure each movie has reviews with a movieID on each that matches the movie

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
            var response = await _movieController.GetMovieReviewFromSearchForm(searchForm);

            var results = ((OkObjectResult)response.Result).Value as MovieReview;

            if (reviewBody != null)
            {
                for (int i = 0; i < results.ReviewDocuments.Count(); i++)
                {
                    var reviews = results.ReviewDocuments[i];
                    foreach (Review reviewResult in reviews)
                    {
                        Assert.That(reviewResult.ReviewBody.ToLower(), Contains.Substring(reviewBody.ToLower()));
                    }
                }
            }

            if (reviewTitle != null)
            {
                for (int i = 0; i < results.ReviewDocuments.Count(); i++)
                {
                    var reviews = results.ReviewDocuments[i];
                    foreach (Review reviewResult in reviews)
                    {
                        Assert.That(reviewResult.ReviewBody.ToLower(), Contains.Substring(reviewTitle.ToLower()));
                    }
                }
            }
        }
    }
}

