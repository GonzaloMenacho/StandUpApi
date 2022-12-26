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
        public string Rating { get; set; }
        public string Description { get; set; }
    }
}