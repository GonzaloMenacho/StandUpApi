using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    public class FieldTerms
    {
        public string field { get; set; }
        public string[] searchTerms { get; set; }
        [DefaultValue(false)]
        public bool isMinMax {get; set;}
        public float maxTerm { get; set; }
        public float minTerm { get; set; }
    }
}