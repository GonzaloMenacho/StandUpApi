using System.Reflection;

namespace API.services
{
    public class ObjectAnalyzer
    {
        public static bool IsAllNullOrEmpty(object myObject)
        {
            foreach (PropertyInfo pi in myObject.GetType().GetProperties())
            {
                if (pi.GetValue(myObject) != null)
                {
                    return false;
                }
            }
            return true;
        }
        public static string printProperties(object form)
        {
            Type type1 = form.GetType();
            string returnString = "Properties for \r\n" + type1.Name.ToString();

            PropertyInfo[] props = type1.GetProperties();

            foreach (PropertyInfo p in props)
            {
                string pName = p.Name;
                if (p.PropertyType.IsArray && p.GetValue(form) != null)
                {
                    Array a = (Array)p.GetValue(form);
                    string arrayElements = "";

                    for (int i = 0; i < a.Length; i++)
                    {
                        object pValue = a.GetValue(i);
                        if (i == a.Length - 1)
                        {
                            arrayElements += pValue.ToString();
                        }
                        else
                        {
                            arrayElements += pValue.ToString() + ", ";
                        }
                    }
                    returnString += System.String.Format("{0} = [{1}]\n", pName, arrayElements);
                }
                else
                {
                    object pValue = p.GetValue(form, null);
                    returnString += System.String.Format("{0} = {1}\n", pName, pValue);
                }
            }
            return returnString;
        }
    }
}
