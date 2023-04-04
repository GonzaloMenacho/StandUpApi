using Nest;
using System.ComponentModel;

namespace API.services
{
    public class AdvancedSearchForm
    {
        // names of field must match dictionaries of each controller (not case sensitive)
        // all numeric fields are floats because the minmax service only accepts floats

        // Movie Fields to search
        [DefaultValue(null)]
        public string MovieTitle { get; set; }
        [DefaultValue(null)]
        public float[] TotalUserRatingMinMax { get; set; } // 0 - 10. MovieIMDbRating
        [DefaultValue(null)]
        public float[] TotalRatingCountMinMax { get; set; }
        //public string TotalUserReviews { get; set; } // i.e., "9.5k". stored as string in dataset
        //public float[] TotalCriticReviewsMinMax { get; set; } // i.e., "593". stored as int in dataset
        [DefaultValue(null)]
        public float[] CriticRatingMinMax { get; set; } // 0 - 100. MetaScore.
        [DefaultValue(null)]
        public string[] MovieGenres { get; set; }
        [DefaultValue(null)]
        public string[] Directors { get; set; }
        //[DefaultValue(null)]
        //public DateTime DatePublished { get; set; }
        [DefaultValue(null)]
        public string[] Creators { get; set; }
        [DefaultValue(null)]
        public string[] MainStars { get; set; }
        [DefaultValue(null)]
        public string Description { get; set; }
        [DefaultValue(null)]
        public float[] DurationMinMax { get; set; } // in minutes

        // Review fields to search
        //public string DateofReview { get; set; }
        [DefaultValue(null)]
        public string movieID { get; set; }
        [DefaultValue(null)]
        public string ReviewBody { get; set; }
        [DefaultValue(null)]
        public string ReviewTitle { get; set; }
        [DefaultValue(null)]
        public float[] TotalVotesMinMax { get; set; }
        [DefaultValue(null)]
        public float[] UsefulnessVoteMinMax { get; set; }
        [DefaultValue(null)]
        public string Username { get; set; }
        [DefaultValue(null)]
        public float[] ReviewUserRatingMinMax { get; set; }
    }
}
