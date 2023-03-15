using Microsoft.Extensions.Logging;
using Nest;

namespace API.services
{
    //TODO: add an nGram tokenizer to have partial word matches (i.e., "ave" should maybe "avengers")
    public class multiFieldMatch
    {
        private static QueryContainer[] MatchListBuilder(Dictionary<string, string[]> fieldsAndSearchTerms)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var field in fieldsAndSearchTerms.Keys)
            {
                foreach (var searchTerm in fieldsAndSearchTerms[field])
                {
                    orQuery = new MatchQuery() { Field = field, Query = searchTerm };
                    queryContainerList.Add(orQuery);
                }
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer MatchRequest<T>(T typeOBJ, Dictionary<string, string[]> fieldsAndSearchTerms) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Should(
                    MatchListBuilder(fieldsAndSearchTerms)));
            return q;
        }
    }
}
