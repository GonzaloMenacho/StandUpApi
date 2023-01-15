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
        public string Title { get; set; }
        public float UserRating { get; set; } // 0 - 10. also known as "movieIMDbRating" in dataset
        public string Description { get; set; }
        public string CriticRating { get; set; } // 0 - 100. also known as "metaScore" in dataset
        public double TotalRatingCount { get; set; }
        public string TotalUserReviews { get; set; } // i.e., "9.5k". stored as string in dataset
        public string TotalCriticReviews { get; set; } // i.e., "593". stored as string in dataset
        public List<string> Genres { get; set; }
        public string DatePublished { get; set; } // i.e., "2019-04-26". stored as string in dataset
        public int Duration { get; set; } // in minutes
        public string MovieTrailer { get; set; }  // youtube link to be embedded
    }
}