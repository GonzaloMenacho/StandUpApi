using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    // movie model class
    public class Review
    {
        // data type reflects json in ES
        public int MovieID { get; set; }
        public int ReviewID { get; set; }
        public string Username { get; set; }
        public string DateofReview { get; set; } // i.e., "2019-04-26". stored as string in dataset
        public string ReviewTitle { get; set; }
        public string ReviewBody { get; set; }
        public int UsersRating { get; set; } // 0 - 10. whole numbers only
        public int UsefulnessVotes { get; set; }
        public int TotalVotes { get; set; }
    }
}