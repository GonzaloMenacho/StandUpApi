using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System.Text.RegularExpressions;

namespace API.services
{
    public class searchByCharRaw
    {

        public static QueryContainer[] RegexpListBuilder(string field, string[] searchTerms)
        {
            QueryContainer orQuery = null;
            List<QueryContainer> queryContainerList = new List<QueryContainer>();
            foreach (var item in searchTerms)
            {
                string m_text = item;
                //pre-processing
                m_text = Regex.Replace(m_text, @"[^\w- ]|_", " ");   //replace illegal chars and underscores with a space
                m_text = m_text.Trim();   // trim leading and ending whitespaces

                // replacing each character in the string with regex that ignores capitalization.
                // i.e. "a" and "A" become "[Aa]"
                string fixed_title = "";
                foreach (char c in m_text)
                {
                    if (c >= 'a' && c <= 'z')
                    {
                        fixed_title += $"[{(char)(c - 32)}{c}]";
                    }
                    else if (c >= 'A' && c <= 'Z')
                    {
                        fixed_title += $"[{c}{(char)(c + 32)}]";
                    }
                    else
                    {
                        fixed_title += $"[{c}]?";   // if " ", "-", or other special character, make it zero or 1
                    }
                }

                // regex to grab every string with the fixed_title as the substring
                m_text = $".*{fixed_title}.*";
                orQuery = new RegexpQuery() { Field = field, Value = m_text };
                queryContainerList.Add(orQuery);
            }
            return queryContainerList.ToArray();
        }

        public static QueryContainer RegexpRequest<T>(string field, T typeOBJ, params string[] searchTerms) where T : class
        {
            // to know what model to use, we need to be passed a class object of that model. Might be a better way?
            var q = new QueryContainerDescriptor<T>().Bool(
                b => b.Should(
                    RegexpListBuilder(field, searchTerms)));
            return q;
        }
    }
}
