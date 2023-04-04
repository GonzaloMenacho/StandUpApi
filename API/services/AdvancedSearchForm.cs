using Nest;

namespace API.services
{
    public class AdvancedSearchForm
    {
        // Movie Fields to search
        public string MovieTitle { get; set; }
        public float[] TotalUserRatingMinMax { get; set; } // 0 - 10. MovieIMDbRating
        public double[] TotalRatingCountMinMax { get; set; }
        //public string TotalUserReviews { get; set; } // i.e., "9.5k". stored as string in dataset
        //public int[] TotalCriticReviewsMinMax { get; set; } // i.e., "593". stored as int in dataset
        public int[] CriticRatingMinMax { get; set; } // 0 - 100. MetaScore.
        public string[] MovieGenres { get; set; }
        public string[] Directors { get; set; }
        public DateTime DatePublished { get; set; }
        public string[] Creators { get; set; }
        public string[] MainStars { get; set; }
        public string Description { get; set; }
        public int[] DurationMinMax { get; set; } // in minutes
        
        // Review fields to search
        //public string DateofReview { get; set; }
        public string ReviewBody { get; set; }
        public string ReviewTitle { get; set; }
        public int[] TotalVotesMinMax { get; set; }
        public int[] UsefulnessVoteMinMax { get; set; }
        public string Username { get; set; }
        public int[] ReviewUserRatingMinMax { get; set; }
    }
}
