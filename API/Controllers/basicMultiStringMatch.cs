using Microsoft.Extensions.Logging;
using Nest;

namespace API.Controllers
{
    public class basicMultiStringMatch
    {
        private static QueryContainer[] MatchListBuilder(string field, string[] searchTerms)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in searchTerms)
            {
                orQuery = new MatchQuery() { Field = field, Query = item };
                queryContainerList.Add(orQuery);
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer ShouldMatchRequest(string field, params string[] searchTerms)
        {
            var q = new QueryContainerDescriptor<Movie>().Bool(
                b => b.Should(
                    MatchListBuilder(field, searchTerms)));
            return q;
        }
    }
}
