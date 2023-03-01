using Microsoft.Extensions.Logging;
using Nest;
using System.Collections.Generic;

namespace API.services
{
    public class minMaxService
    {
        public Type GetOBJType<T>(T typeObject) where T : class
        {
            return typeObject.GetType();
        }

        private static QueryContainer[] RangeQueryBuilder(string field, float minNum, float maxNum)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            orQuery = new NumericRangeQuery() { Field = field, GreaterThanOrEqualTo = minNum, LessThanOrEqualTo = maxNum };
            queryContainerList.Add(orQuery);
            return queryContainerList.ToArray();
        }

        public static QueryContainer RangeRequest<T>(string field, T typeOBJ, float minNum, float maxNum) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Should(
                    RangeQueryBuilder(field, minNum, maxNum)));
            return q;
        }
    }
}
