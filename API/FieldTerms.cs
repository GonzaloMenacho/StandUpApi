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

        //public FieldTerms() { }
    }

    //public class CustomModelBinder : IModelBinder
    //{
    //    public Task BindModelAsync(ModelBindingContext bindingContext)
    //    {
    //        if (bindingContext == null)
    //        {
    //            throw new ArgumentNullException(nameof(bindingContext));
    //        }

    //        var values = bindingContext.ValueProvider.GetValue("searchTerms");

    //        if (values.Length == 0)
    //        {
    //            return Task.CompletedTask;
    //        }

    //        var splitData = values.FirstValue.Split(',');
    //        var result = new FieldTerms()
    //        {
    //            searchTerms = new List<int>()
    //        };

    //        foreach (var id in splitData)
    //        {
    //            result.StateList.Add(int.Parse(id));
    //        }
    //        bindingContext.Result = ModelBindingResult.Success(result);
    //        return Task.CompletedTask;
    //    }
    //}
}