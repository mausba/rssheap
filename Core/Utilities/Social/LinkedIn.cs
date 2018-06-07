using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Core.Utilities
{
    public static class LinkedIn
    {
        public static int GetNumberOfShares(string url)
        {
            try
            {
                var linkedInUrl = "http://www.linkedin.com/countserv/count/share?url=" + HttpUtility.UrlEncode(url) + "&callback=myCallback&format=json";
                var wc = new WebClient();
                var data = wc.DownloadString(linkedInUrl);

                return JObject.Parse(data)["count"].Value<int>();
            }
            catch
            {
                return 0;
            }
        }
    }
}
