using Microsoft.Extensions.Logging;
using Nest;

namespace API.services
{
    public class multiMatchService
    {
        public static QueryContainer[] MultiMatchListBuilder(string[] fields, string[] searchTerms, bool fuzz = true)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in searchTerms)
            {
                if (item.Length <= 3) { fuzz = false;}
                if (fuzz)
                {
                    orQuery = new MultiMatchQuery()
                    {
                        Fields = fields,
                        Query = item,
                        Fuzziness = Fuzziness.Auto,
                        Type = TextQueryType.BestFields // prioritizes multiple words being found in a field more than one word appearing in multiple
                    };
                }
                else
                {
                    orQuery = new MultiMatchQuery()
                    {
                        Fields = fields,
                        Query = item,
                        Type = TextQueryType.BestFields
                    };
                }
                queryContainerList.Add(orQuery);
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer MultiMatchRequest<T>(string[] fields, T typeOBJ, params string[] searchTerms) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Should(
                    MultiMatchListBuilder(fields, searchTerms)));
            return q;
        }
    }
}
