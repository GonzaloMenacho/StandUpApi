using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    public class FieldTerms
    {
        public string field { get; set; }
        public string[] searchTerms { get; set; }

    }
}