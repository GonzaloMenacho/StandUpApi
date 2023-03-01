using Microsoft.Extensions.Logging;
using Nest;

namespace API.Controllers
{
    public class QueryBuilder
    {
        private static QueryContainer[] InnerBlah(string field, string[] param)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in param)
            {
                orQuery = new MatchQuery() { Field = field, Query = item };
                queryContainerList.Add(orQuery);
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer Blah(string field, params string[] param)
        {
            var q = new QueryContainerDescriptor<Movie>().Bool(
                b => b.Must(
                    InnerBlah(field, param)));
            return q;
            //return new QueryContainerDescriptor<Movie>().Match(m => InnerBlah(field, param)));
        }
    }
}


//q.Match(m => m.Field(f => f.MovieIMDbRating).Query(specificRating)