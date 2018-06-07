using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Parsers;
using Core.Utilities;
using Core.Services;
using Core.Models;
using System.Text.RegularExpressions;
using System.IO;
using Core.Data;
using System.Data;
using Core.Caching;

namespace Core.Tests
{
    [TestClass]
    public class ParsingTests
    {
        private FeedService FeedService = new FeedService();
        private DataProvider dp = new DataProvider();

        [TestMethod]
        public void DeleteUser()
        {
            int user = 123;

            var s = "delete from Newsletter where UserId = " + user + ";";
            s += "delete from UserArticleVote where UserId = " + user + ";";
            s += "delete from UserFavoriteArticle where UserId = " + user + ";";
            s += "delete from UserFeed where UserId = " + user + ";";
            s += "delete from UserReadArticle where UserId = " + user + ";";
            s += "delete from Newsletter where UserId = " + user + ";";
            s += "delete from UserArticleIgnored where UserId = " + user + ";";
            s += "delete from User where Id = " + user + ";";

            new DataProvider().GetFromSelect(s);
            Redis.DeleteUser(user);
        }

        [TestMethod]
        public void ParseLocalFeed()
        {
            var ids = new List<int>();
            ids.Add(36641);
            foreach (var id in ids)
            {
                new FeedParser().Parse(new FeedService().GetFeed(id));
            }
        }

        [TestMethod]
        public void FixIndexing()
        {
            new LuceneSearch().DeleteNotIndexedIn(DateTime.Now.AddDays(-7));
        }

        [TestMethod]
        public void ParseAll()
        {
            var lFeeds = new FeedService().GetFeeds().Where(f => f.Updated <= DateTime.Now.AddDays(1)).ToList();
            foreach (var lFeed in lFeeds)
            {
                try
                {
                    new FeedParser().Parse(lFeed);
                }
                catch
                {

                }
            }
        }

        [TestMethod]
        public void ParseArticleShortUrl()
        {
            var feeds = FeedService.GetFeeds();
            foreach (var feed in feeds)
            {
                var articles = FeedService.GetArticlesWithoutBody(feed.Id);
                foreach (var article in articles)
                {
                    if (article.ShortUrl.IsNullOrEmpty())
                    {
                        //article.ShortUrl = article.GetShortUrl();
                        FeedService.UpdateArticle(article);
                    }
                }
            }
        }

        [TestMethod]
        public void SendAnEmail()
        {
            Mail.SendMeAnEmail("test", "testbody");
        }

        [TestMethod]
        public void WordCounter()
        {
            var categories = FeedService.GetTags().Where(e => e.ArticlesCount > 3).ToList();
            var feeds = FeedService.GetFeeds();
            foreach (var feed in feeds)
            {
                var articles = FeedService.GetArticlesWithoutBody(feed.Id);
                foreach (var article in articles)
                {
                    var wordOccurences = article.Body
                                              .GetWordOccurences()
                                              .Where(w => w.Value >= 10)
                                              .Select(w => w.Key.ToLower())
                                              .ToList();

                    var appliedCats = categories.FindAll(c => wordOccurences.Contains(c.Name.ToLower()));
                }
            }
        }

        [TestMethod]
        public void ParseTags()
        {
            new TagParser().ParseTags();
        }

        [TestMethod]
        public void ParseRedditVotes()
        {
            var entries = FeedService.GetArticles();
            foreach (var article in entries)
            {
                var count = Reddit.GetNumberOfVotes(article.Url);
                if (count > 0)
                {
                    article.LikesCount += count;
                    FeedService.UpdateArticle(article);
                }
            }
        }
    }
}
