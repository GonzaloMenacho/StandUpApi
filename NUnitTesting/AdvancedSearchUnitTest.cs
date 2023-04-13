using API;
using API.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NUnitTesting
{
    [TestFixture]
    internal class AdvancedSearchUnitTest : AbstractTestBase
    {
        [Test]
        [TestCase(null, null, null, null, null, null, null, null)]
        [TestCase("avengers", null, null, null, null, null, null, null)]
        [TestCase("forrest gump", null, null, null, null, null, null, null)]
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

        [Test]
        [TestCase(null, null, null, "awesome", null, null, null, null)]
        [TestCase(null, null, null, null, "iron", null, null, null)]
        [TestCase(null, null, null, "awesome", "iron", null, null, null)]
        [TestCase(null, null, null, "awesome movie", "iron man", null, null, null)]
        [TestCase(null, null, null, "awesome movie", "awesome movie", null, null, null)] // this one should allow for the keyword to appear in either one instead of both
        [TestCase(null, null, null, "morbillion", "morbillion", null, null, null)]
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
            // Asserts that if we query a review field with a set of key words, it has at least one of those words within that field.
            // Assumes no fuzziness used on the words queried.

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

            bool searchingWithMultiMatch = false;

            if (reviewBody == reviewTitle) 
            { 
                searchingWithMultiMatch = true; 
            }

            int keywordsPresent = 0;
            if (reviewBody != null)
            {
                string[] bodyKeywords = reviewBody.Split(' ');
                for (int i = 0; i < results.ReviewDocuments.Count(); i++)
                {
                    var reviews = results.ReviewDocuments[i];
                    foreach (Review reviewResult in reviews)
                    {
                        foreach (string keyword in bodyKeywords)
                        {
                            //Assert.That(reviewResult.ReviewBody.ToLower(), Contains.Substring(keyword.ToLower()));
                            if (reviewResult.ReviewBody.ToLower().Contains(keyword.ToLower()))
                            {
                                keywordsPresent++;
                            }
                        }
                    }
                    if (!searchingWithMultiMatch)
                    {
                        Assert.That(keywordsPresent, Is.GreaterThanOrEqualTo(1));
                        keywordsPresent = 0;
                    }
                }
            }

            if (reviewTitle != null)
            {
                string[] titleKeywords = reviewTitle.Split(' ');
                for (int i = 0; i < results.ReviewDocuments.Count(); i++)
                {
                    var reviews = results.ReviewDocuments[i];
                    foreach (Review reviewResult in reviews)
                    {
                        foreach (string keyword in titleKeywords)
                        {
                            //Assert.That(reviewResult.ReviewTitle.ToLower(), Contains.Substring(keyword.ToLower()));
                            if (reviewResult.ReviewTitle.ToLower().Contains(keyword.ToLower()))
                            {
                                keywordsPresent++;
                            }
                        }
                    }
                    if (!searchingWithMultiMatch)
                    {
                        Assert.That(keywordsPresent, Is.GreaterThanOrEqualTo(1));
                        keywordsPresent = 0;
                    }
                }
            }

            if (searchingWithMultiMatch)        // multimatch allows for the keyword to show up in either field
            {
                Assert.That(keywordsPresent, Is.GreaterThanOrEqualTo(1));
            }
        }

        [Test]
        [TestCase(new[] { "action" }, null, null, null)]
        [TestCase(new[] { "action", "drama" }, null, null, null)]
        [TestCase(new[] { "action drama" }, null, null, null)]
        [TestCase(null, new[] { "russo" }, null, null)]
        [TestCase(null, new[] { "russo", "joe" }, null, null)]
        [TestCase(null, new[] { "anthony russo" }, null, null)]
        [TestCase(null, new[] { "anthony russo", "joe russo" }, null, null)]
        [TestCase(null, null, new[] { "tarantino" }, null)]
        [TestCase(null, null, new[] { "tarantino", "avery" }, null)]
        [TestCase(null, null, new[] { "stan lee" }, null)]
        [TestCase(null, null, new[] { "Mckenna", "stan lee" }, null)]
        [TestCase(null, null, null, new[] { "holland" })]
        [TestCase(null, null, null, new[] { "tom holland" })]
        [TestCase(null, null, null, new[] { "bale", "ledger" })]
        [TestCase(null, null, null, new[] { "bale", "heath ledger" })]
        [TestCase(new[] { "action", "drama" }, new[] { "anthony russo", "joe russo" }, new[] { "stan lee", "Stephen McFeely" }, new[] { "Robert Downey Jr.", "Chris Evans", "Mark Ruffalo" })]
        public async Task TestAdvancedSearchOnStringArrayFields(
            string[] genresList,
            string[] directorsList,
            string[] creatorsList,
            string[] mainStarsList
            )
        {
            // Arrange
            var searchForm = new AdvancedSearchForm
            {
                MovieGenres = genresList,
                Directors = directorsList,
                Creators = creatorsList,
                MainStars = mainStarsList
            };

            // Act
            var response = await _movieController.GetMovieReviewFromSearchForm(searchForm);
            var results = ((OkObjectResult)response.Result).Value as MovieReview;

            string[][] listsPerField = new[] { genresList, directorsList, creatorsList, mainStarsList };

            // Because string array fields only exist in the movie attributes, we can do this:
            var moviesList = results.MovieDocuments;
            for (int i = 0; i < moviesList.Count(); i++)
            {
                for (int fieldIndex = 0; fieldIndex < listsPerField.Length; fieldIndex++) // loop through each field list
                {
                    string[] keywordList = listsPerField[fieldIndex];        // grab current list (i.e., genresList)
                    if (keywordList != null)
                    {
                        int keywordPresent = 0;
                        foreach (string setOfKeywords in keywordList)   // for each string in the current list
                        {
                            string[] innerKeywordList = setOfKeywords.Split(' ');   // split that string into words
                            foreach (string singleKeyword in innerKeywordList)    // for each word, check to see if it is present in the field array
                            {
                                switch (fieldIndex)
                                {
                                    case 0:
                                        foreach (string genre in moviesList[i].MovieGenres)
                                        {
                                            if (genre.Contains(singleKeyword.ToLower(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                keywordPresent++;
                                            }
                                        }
                                        break;
                                    case 1:
                                        foreach (string director in moviesList[i].Directors)
                                        {
                                            if (director.Contains(singleKeyword.ToLower(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                keywordPresent++;
                                            }
                                        }
                                        break;
                                    case 2:
                                        foreach (string creator in moviesList[i].Creators)
                                        {
                                            if (creator.Contains(singleKeyword.ToLower(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                keywordPresent++;
                                            }
                                        }
                                        break;
                                    case 3:
                                        foreach (string mainStar in moviesList[i].MainStars)
                                        {
                                            if (mainStar.Contains(singleKeyword.ToLower(), StringComparison.OrdinalIgnoreCase))
                                            {
                                                keywordPresent++;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        Assert.That(keywordPresent, Is.GreaterThanOrEqualTo(1));
                    }
                }
            }
        }
    }
}
