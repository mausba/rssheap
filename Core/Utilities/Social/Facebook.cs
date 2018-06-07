using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core.Utilities
{
    public static class Facebook
    {
        public static int GetNumberOfLikes(string url)
        {
            if (url.IsNullOrEmpty()) return 0;
            try
            {
                string facebookUrl = "https://graph.facebook.com/v2.7/?id=" + HttpUtility.UrlEncode(url);
                facebookUrl += "&access_token=685201244886528|6FgwL2O_KArlXvzKGLqQ5vj08LY";

                var wc = new WebClient();
                string data = wc.DownloadString(facebookUrl);

                var json = JObject.Parse(data);
                if (json == null) return 0;

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

                return json["share"]["share_count"].Value<int>();
            }
            catch { }
            return 0;
        }
    }
}
