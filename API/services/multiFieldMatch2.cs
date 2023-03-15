using Microsoft.Extensions.Logging;
using Nest;

namespace API.services
{
    //TODO: add an nGram tokenizer to have partial word matches (i.e., "ave" should maybe "avengers")
    public class multiFieldMatch2
    {
        private static QueryContainer[] MatchListBuilder(List<FieldTerms> fieldTerms)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var query in fieldTerms)
            {
                foreach (var searchTerm in query.searchTerms)
                {
                    orQuery = new MatchQuery() { Field = query.field, Query = searchTerm };
                    queryContainerList.Add(orQuery);
                }
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer MatchRequest<T>(T typeOBJ, List<FieldTerms> fieldTerms) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Must(
                    MatchListBuilder(fieldTerms)));
            return q;
        }
    }
}
