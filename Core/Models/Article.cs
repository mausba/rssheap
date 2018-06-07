using Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace Core.Models
{
    [DebuggerDisplay("{Id} {Name}")]
    public class Article : Metadata
    {
        public Article()
        {
            Tags = new List<Tag>();
            TagsLoaded = false;
            FlaggedBy = new List<int>();
        }

        public int Id { get; set; }
        public int FeedId { get; set; }
        public Feed Feed { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public int ViewsCount { get; set; }
        public int LikesCount { get; set; }
        public int FavoriteCount { get; set; }
        public DateTime Published { get; set; }

        private string _Tweet;
        public string Tweet
        {
            get
            {
                if (_Tweet.IsNullOrEmpty())
                {
                    var url = "http://rssheap.com/a/" + ShortUrl;
                    var twitterTitle = Name;
                    if (twitterTitle.Length > 100)
                    {
                        twitterTitle = twitterTitle.Substring(0, 100) + "...";
                    }
                    twitterTitle += " " + url + " via @rssheap ";
                    _Tweet = twitterTitle;
                }
                return _Tweet;
            }
        }

        private string _TimeAgo;
        public string TimeAgo
        {
            get
            {
                if (_TimeAgo == null)
                {
                    var days = Convert.ToInt32(Math.Abs((DateTime.Now - Published).TotalDays));
                    if (days < 1) _TimeAgo = Convert.ToInt32(Math.Abs((DateTime.Now - Published).TotalHours)) + "h";
                    else if (days <= 7) _TimeAgo = days + "d";
                    else if (days > 7 && days <= 30) _TimeAgo = Convert.ToInt32(days / 7) + "w";
                    else if (days > 30 && days <= 365) _TimeAgo = Convert.ToInt32(days / 30) + "m";
                    else if (days > 365) _TimeAgo = Convert.ToInt32(days / 365) + "y";
                    _TimeAgo = string.Empty;
                }
                return _TimeAgo;
            }
        }

        public string TimeAgoLong
        {
            get { return Published.TimeAgo(); }
        }

        public DateTime Created { get; set; }
        public DateTime Indexed { get; set; }
        public string ShortUrl { get; set; }

        public bool Flagged { get; set; }
        public List<int> FlaggedBy { get; set; }

        public bool TagsLoaded { get; set; }
        private List<Tag> _Tags;
        public List<Tag> Tags
        {
            get
            {
                return _Tags;
            }
            set
            {
                TagsLoaded = true;
                _Tags = value;
            }
        }

        private string _PrettyUrl;
        public string PrettyUrl
        {
            get
            {
                if (_PrettyUrl.IsNullOrEmpty())
                {
                    _PrettyUrl = "/articles/" + Id + "/" + Name.GenerateSlug();
                }
                return _PrettyUrl;
            }
        }

        public int MyVotes { get; set; }
        public bool IsMyFavorite { get; set; }

        public string GetHref(ArticlesRequest request)
        {
            return GetHref(request, null);
        }

        /// <summary>
        /// Used to inject the user guid in the query string (for example, newsletter links)
        /// </summary>
        public string GetHref(ArticlesRequest request, string guid)
        {
            var url = "/article/?p=";
            var query = "articleid=" + Id;
            if (request.Week)
                query += "&tab=week";

            if (request.Month)
                query += "&tab=month";

            if (request.FolderId > 0)
                query += "&tab=folder";

            if (request.Untaged)
                query += "&tab=untaged";

            if (request.Favorites)
                query += "&tab=favorite";

            if (request.Votes)
                query += "&tab=votes";

            if (request.MyFeeds)
                query += "&tab=myfeeds";

            if (request.FeedId > 0)
                query += "&feed=" + request.FeedId;

            if (request.FolderId > 0)
                query += "&folder=" + request.FolderId;

            if (!guid.IsNullOrEmpty())
                query += "&guid=" + guid;

            if (request.TagId > 0)
                query += "&tag=" + request.TagId;

            query += "&filter=" + request.Filter;

            return url + Convert.ToBase64String(Encoding.UTF8.GetBytes(query));
        }

        public string GetHrefForNext(NameValueCollection q)
        {
            return "/article/?p=" +
                (
                "next=true&articleid=" + Id +
                "&tab=" + q["tab"] +
                "&folder=" + q["folder"] +
                "&feed=" + q["feed"] +
                "&tag=" + q["tag"] +
                "&filter=" + q["filter"] +
                "&guid=" + q["guid"]
                ).ToBase64();
        }

        protected override MetaEntity GetMetaEntity()
        {
            return new MetaEntity
            {
                EntityId = Id,
                EntityName = "article"
            };
        }
    }
}
