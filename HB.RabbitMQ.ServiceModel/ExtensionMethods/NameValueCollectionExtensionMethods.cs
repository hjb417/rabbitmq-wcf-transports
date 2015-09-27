using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace HB
{
    public static class NameValueCollectionExtensionMethods
    {
        public static bool ContainsKey(this NameValueCollection nameValueCollection, string key)
        {
            foreach(string existingKey in nameValueCollection.Keys)
            {
                if(StringComparer.OrdinalIgnoreCase.Equals(key, existingKey))
                {
                    return true;
                }
            }
            return false;
        }

        public static string ToQueryString(this NameValueCollection nameValueCollection)
        {
            var qs = new StringBuilder();
            foreach (string key in nameValueCollection.Keys)
            {
                foreach (string value in nameValueCollection.GetValues(key))
                {
                    qs.AppendFormat("&{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value));
                }
            }
            if(qs.Length > 0)
            {
                qs.Remove(0, 1);
            }
            return qs.ToString();
        }
    }
}
