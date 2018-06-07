using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Core.Utilities
{
    public static class Reddit
    {
        public static int GetNumberOfVotes(string url)
        {
            if (url.IsNullOrEmpty()) return 0;
            try
            {
                string redditUrl = "http://www.reddit.com/api/info.json?url=" + HttpUtility.UrlEncode(url);

                var wc = new WebClient();
                wc.Headers["User-Agent"] = "www.rssheap.com";
                string data = wc.DownloadString(redditUrl);

                var count = 0;
                var json = JObject.Parse(data);
                var list = json["data"]["children"] as JArray;
                foreach (var item in list)
                {
                    count += item["data"]["ups"].Value<int>() - item["data"]["downs"].Value<int>();
                }
                return count;
                //var jsonArray = JArray.Parse();
                //if (jsonArray.Count == 0) return 0;

                //[{
                ////"url":"lingvisti.ba",
                ////"normalized_url":"http:\/\/www.lingvisti.ba\/",
                ////"share_count":35,
                ////"like_count":20,
                ////"comment_count":4,
                ////"total_count":59,
                ////"click_count":7,
                ////"comments_fbid":10150107613460899,
                ////"commentsbox_count":0
                //}]

                //return jsonArray[0]["total_count"].Value<int>();
            }
            catch { }
            return 0;
        }
    }
}
