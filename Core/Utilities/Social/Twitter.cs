using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Core.Utilities
{
    public static class Twitter
    {
        public static int GetNumberOfTweets(string url)
        {
            try
            {
                var wc = new WebClient();
                var data = wc.DownloadString("http://cdn.api.twitter.com/1/urls/count.json?url=" + HttpUtility.UrlEncode(url));

                var jsonObj = JObject.Parse(data);
                return jsonObj["count"].Value<int>();
            }
            catch { }
            return 0;
        }
    }
}
