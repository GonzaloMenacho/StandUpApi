﻿using Microsoft.Extensions.Logging;
using Nest;

namespace API.services
{
    public class matchService
    {
        public static QueryContainer[] MatchListBuilder(string field, string[] searchTerms, bool fuzz = true)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in searchTerms)
            {
                if (item.Length <= 3) { fuzz = false;}
                if (fuzz)
                {
                    orQuery = new MatchQuery()
                    {
                        Field = field,
                        Query = item,
                        Fuzziness = Fuzziness.Auto
                    };
                }
                else
                {
                    orQuery = new MatchQuery()
                    {
                        Field = field,
                        Query = item
                    };
                }
                queryContainerList.Add(orQuery);
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer MatchRequest<T>(string field, T typeOBJ, params string[] searchTerms) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Should(
                    MatchListBuilder(field, searchTerms)));
            return q;
        }
    }
}
