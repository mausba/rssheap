using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace Core.Models
{
    public class FeedEntry
    {
        public FeedEntry()
        {
            CategoryIds = new List<int>();
            UserLikesIds = new Dictionary<int, string>();
        }

        public int Id { get; set; }
        public string RemoteId { get; set; }
        public int FeedId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Body { get; set; }
        public string Url { get; set; }
        public int ViewsCount { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime Published { get; set; }
        public DateTime Created { get; set; }

        public string ImageUrl
        {
            get 
            {
                string defaultImagePath = "/images/feeds/default.png";
                if (string.IsNullOrEmpty(Body)) return defaultImagePath;

                var doc = new HtmlDocument();
                doc.LoadHtml(Body);

                var nodes = doc.DocumentNode.SelectNodes("//img");
                if (nodes == null) return defaultImagePath;
                if (nodes.Count == 0) return defaultImagePath;
                if (nodes.Count == 1) return nodes[0].Attributes["src"].Value;

                string imageSource = string.Empty;
                int maxWidth = 0;
                foreach (var node in nodes)
                {
                    if (node.Attributes["width"] == null) continue;

                    string width = node.Attributes["width"].Value;

                    if (int.Parse(width) > maxWidth)
                    {
                        maxWidth = int.Parse(width);
                        imageSource = node.Attributes["src"].Value;
                    }
                }

                if (string.IsNullOrEmpty(imageSource))
                    return nodes.First().Attributes["src"].Value;
                return imageSource;
            }
        }

        public List<int> CategoryIds { get; set; }
        public Dictionary<int, string> UserLikesIds { get; set; }

        public object Tag { get; set; }

        public bool UserLikedIt(int userId)
        {
            return UserLikesIds.ContainsKey(userId);
        }

        public void AddUserLike(int userId, string userName)
        {
            if (!UserLikedIt(userId))
            {
                UserLikesIds.Add(userId, userName);
            }
        }
    }
}
