using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Services;
using Core.Models;
using System.Xml;
using System.ServiceModel.Syndication;
using System.IO;
using Core.Utilities;
using System.Net;
using System.Web;
using System.Threading;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace Core.Parsers
{
    public class FeedParser
    {
        private FeedService FeedService { get; set; }
        private LuceneSearch LuceneSearch { get; set; }

        public FeedParser()
        {
            FeedService = new FeedService();
            LuceneSearch = new LuceneSearch();
        }

        public void ParseAllFeeds()
        {
            var feeds = FeedService.GetFeeds().Where(f => f.Updated <= DateTime.Now.AddDays(-1));

            foreach (var feed in feeds)
            {
                Parse(feed.Id);
            }
        }

        public void Parse(int id)
        {
            var lFeed = FeedService.GetFeed(id);
            if (lFeed == null) return;

            var rFeed = FeedService.GetRemoteFeed(lFeed.Url);
            if (rFeed == null)
            {
                lFeed.Updated = DateTime.Now;
                FeedService.UpdateFeed(lFeed);
                return;
            }

            lFeed.Articles = FeedService.GetArticles(lFeed.Id);

            var lTags = CacheClient.Default.GetOrAdd<List<Tag>>
                ("parsingLocalTags", CachePeriod.ForMinutes(15), () => FeedService.GetTagsByArticlesCount(100));
            foreach (var rArticle in rFeed.Articles)
            {
                rArticle.Tags = GetArticleTags(rArticle, lTags);
                try
                {
                    var lArticle = lFeed.Articles.Find(le => le.Name == rArticle.Name);
                    if (lArticle == null)
                    {
                        rArticle.FeedId = lFeed.Id;
                        rArticle.LikesCount = Facebook.GetNumberOfLikes(rArticle.Url) +
                                              Twitter.GetNumberOfTweets(rArticle.Url) +
                                              LinkedIn.GetNumberOfShares(rArticle.Url) +
                                              Google.GetNumberOfShares(rArticle.Url) +
                                              Reddit.GetNumberOfVotes(rArticle.Url);

                        rArticle.Tags = GetArticleTags(rArticle, lTags);

                        rArticle.Id = FeedService.InsertArticle(rArticle);
                        rArticle.Feed = lFeed;
                        LuceneSearch.AddUpdateIndex(rArticle);
                    }
                }
                catch (Exception ex)
                {
                    Mail.SendMeAnEmail("Error in insert article", "FeedId: " + lFeed.Id + " " + ex.ToString());
                }
            }

            //validation not to import single article more than once
            var lArticles = FeedService.GetArticles(lFeed.Id);
            foreach (var rArticle in rFeed.Articles)
            {
                var lArticle = lArticles.Find(le => le.Name == rArticle.Name);
                if (lArticle == null)
                {
                    //we have a problem
                    Mail.SendMeAnEmail("Parse feed multiple articles will be inserted", "Local feedId: " + lFeed.Id);
                }
            }
            lFeed.Updated = DateTime.Now;
            FeedService.UpdateFeed(lFeed);
        }

        private List<Tag> GetArticleTags(Article article, List<Tag> tags)
        {
            var name = article.Name.ToLower();
            var keywords = article.Body.GetWordOccurences()
                                  .Where(w => w.Value >= 5)
                                  .Select(w => w.Key.ToLower())
                                  .ToList();

            var html = new HtmlDocument();
            html.Load(new StringReader(article.Body));
            var categories = html.DocumentNode
                                 .Descendants("a")
                                 .Where(l => l.Attributes["href"] != null &&
                                             !string.IsNullOrEmpty(l.Attributes["href"].Value) &&
                                             l.Attributes["href"].Value.Contains("tags") &&
                                             !string.IsNullOrEmpty(l.InnerText))
                                 .Select(l => l.InnerText.ToLower())
                                 .ToList();

            var newTags = new List<Tag>();

            var nameWords = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.ToLower())
                                .ToList();
            foreach (var tag in tags)
            {
                tag.Approved = true;
                var tagName = tag.Name.Replace("-", " ");
                if (nameWords.Contains(tagName))
                {
                    newTags.Add(tag);
                }

                if (keywords.Contains(tagName))
                {
                    newTags.Add(tag);
                }

                if (categories.Contains(tagName))
                {
                    newTags.Clear();
                    newTags.Add(tag);
                }
            }
            return newTags.OrderByDescending(t => t.ArticlesCount).Distinct().Take(4).ToList();
        }
    }
}
