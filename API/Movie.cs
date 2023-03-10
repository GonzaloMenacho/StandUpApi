using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    // movie model class
    public class Movie
    {
        // data type reflects json in ES
        public int MovieID { get; set; } // movieID
        public string Title { get; set; }
        public float MovieIMDbRating { get; set; } // 0 - 10. UserRating
        public double TotalRatingCount { get; set; }
        public string TotalUserReviews { get; set; } // i.e., "9.5k". stored as string in dataset
        public int TotalCriticReviews { get; set; } // i.e., "593". stored as int in dataset
        public int MetaScore { get; set; } // 0 - 100. CriticRating.
        public string[] MovieGenres { get; set; }
        public string[] Directors { get; set; }
        public DateTime DatePublished { get; set; }
        [Date(Name = "@timestamp")]
        public DateTime Timestamp { get; set; } 
        public string[] Creators { get; set; }
        public string[] MainStars { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } // in minutes
        public string MovieTrailer { get; set; }  // youtube link to be embedded
        public string MoviePoster { get; set; }
    }
}