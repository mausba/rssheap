using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Services;
using Core.Models.Requests;
using Core.Utilities;
using System.Net;
using Core.Models;
using System.IO;
using System.Xml;
using System.Threading;

namespace Core.Tests
{
    /// <summary>
    /// Summary description for GetTests
    /// </summary>
    [TestClass]
    public class GetTests
    {
        private FeedService FeedService = new FeedService();
        private UserService UserService = new UserService();

        [TestMethod]
        public void CheckArticleTable()
        {
            var lines = string.Empty;
            var line = string.Empty;
            var counter = 0;
            using (var fs = new StreamReader("D:\\rss_com_db_Article.sql"))
            {
                
                while((line = fs.ReadLine()) != null && counter < 60)
                {
                    counter++;
                    lines += line + Environment.NewLine;
                }
            }
        }

        [TestMethod]
        public void GetLikesCount()
        {
            var url = "http://www.smashingmagazine.com/2014/12/10/mastering-fireworks-css-properties-panel-css-professionalzr/#comment-1252863";

            var likesCount = Facebook.GetNumberOfLikes(url);
            var likesCountTw = Twitter.GetNumberOfTweets(url);
            var likesCountLi = LinkedIn.GetNumberOfShares(url);
            var likesCountGoo = Google.GetNumberOfShares(url);
            var likesCountRed = Reddit.GetNumberOfVotes(url);
        }

        [TestMethod]
        public void UpdateLikesCount()
        {
            var articles = new FeedService().GetArticlesWithoutBody(32997);
            foreach (var a in articles)
            {
                a.LikesCount = Facebook.GetNumberOfLikes(a.Url) +
                                Twitter.GetNumberOfTweets(a.Url) +
                              LinkedIn.GetNumberOfShares(a.Url) +
                               Google.GetNumberOfShares(a.Url) +
                                Reddit.GetNumberOfVotes(a.Url);
                new FeedService().UpdateArticle(a);
            }
        }


        [TestMethod]
        public void GetUser()
        {
            string userName = "test";
            string password = "test";

            var user = new UserService().GetUser(userName, password);
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public void LoadTest()
        {
            var asfd = new FeedService().GetArticles(new ArticlesRequest
            {
                Month = true,
                User = new UserService().GetUser(25),
                PageSize = 90
            });
        }
    }
}
