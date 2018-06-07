using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using Core.Services;
using Core.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core.Parsers
{
    public class TagParser
    {
        private FeedService FeedService = new FeedService();

        public void ParseTags()
        {
            var tags = FeedService.GetTags().OrderBy(t => t.ArticlesCount);
            foreach (var tag in tags)
            {
                FeedService.UpdateTagArticleCount(tag.Id);
            }
        }

        private string GetDescriptionFromStackOverflow(string tagName)
        {
            var url = "https://api.stackexchange.com/2.2/tags/" + HttpUtility.UrlEncode(tagName) + "/wikis?site=stackoverflow";
            var json = string.Empty;

            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                {
                    var sr = new StreamReader(responseStream);
                    json = sr.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }

            var obj = JsonConvert.DeserializeObject(json) as JObject;
            if (!(obj["items"] is JArray itemsObj) || itemsObj.Count == 0 || itemsObj[0]["excerpt"] == null) return string.Empty;

            return itemsObj[0]["excerpt"].Value<string>();
        }
    }
}
