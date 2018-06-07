using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace Core.Models
{
    [DebuggerDisplay("Id: {Id}, Name: {Name}")]
    public class Feed
    {
        public Feed()
        {
            Articles = new List<Article>();
        }

        public int Id { get; set; }
        public string Author { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }

        private string _Url;
        public string Url
        {
            get { return _Url; }
            set
            {
                if (value.IsNullOrEmpty()) return;
                if (value.Contains("feedburner.com"))
                {
                    value = value.Replace("feeds2.feedburner.com", "feeds.feedburner.com");
                }
                _Url = value;
            }
        }

        public string SiteUrl { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public int TotalArticles { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public List<Article> Articles { get; set; }
        public bool Public { get; set; }
        public bool Reviewed { get; set; }

        public Encoding Encoding { get; set; }

        public string PrettyUrl { get { return "/feeds/" + Id + "/" + Name.GenerateSlug(); } }

        public string Favicon { 
            get 
            {
                var url = SiteUrl;
                if (url.IsNullOrEmpty()) url = Url;

                return "http://cdn.rssheap.com/favicon?domain=" + HttpUtility.UrlEncode(url);
            } 
        }
    }
}
