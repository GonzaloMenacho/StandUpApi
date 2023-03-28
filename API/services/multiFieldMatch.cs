using Microsoft.Extensions.Logging;
using Nest;

namespace API.services
{
    //TODO: add some way to handle both movies and reviews in one advanced search. How do we grab only reviews of movies that meet our specified criteria?
    // Ideas: search on the movie criteria first, then add a query on the reviews that says "grab only reviews from these movieIDs"
    public class multiFieldMatch
    {
        private static QueryContainer[] MatchListBuilder(List<FieldTerms> fieldTerms)
        {
            //QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var query in fieldTerms)
            {
                if (query.field == "title") // movie titles should search by Char
                {
                    queryContainerList.AddRange(searchByCharRaw.RegexpListBuilder(query.field, query.searchTerms));
                }
                else if (!query.isMinMax)
                {
                    queryContainerList.AddRange(matchService.MatchListBuilder(query.field, query.searchTerms));
                }
                else
                {
                    queryContainerList.AddRange(minMaxService.RangeQueryBuilder(query.field, query.minTerm, query.maxTerm));
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
