using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    // revuew model class
    public class Review
    {
        // data type reflects json in ES
        // property name matches the name of the field within ES
        [PropertyName("Date of Review")]
        public string DateofReview { get; set; }

        [PropertyName("movieID")]
        public int MovieID { get; set; }

        [PropertyName("Review")]
        public string ReviewBody { get; set; }

        [PropertyName("Review Title")]
        public string ReviewTitle { get; set; }

        [PropertyName("Total Votes")]
        public int TotalVotes { get; set; }

        [PropertyName("Usefulness Vote")]
        public int UsefulnessVote { get; set; }

        [PropertyName("User")]
        public string Username { get; set; }

        [PropertyName("User's Rating out of 10")]
        public int UserRating { get; set; }
    }
}