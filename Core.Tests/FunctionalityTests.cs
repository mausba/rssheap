using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Services;
using Core.Utilities;
using System.Xml;
using System.Net;
using System.IO;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using Core.Models;
using Core.Data;
using System.Data;

namespace Core.Tests
{
    [TestClass]
    public class FunctionalityTests
    {
        public FeedService FeedService = new FeedService();
        public UserService UserService = new UserService();

        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void TestOrigin()
        {
            var dp = new DataProvider();
            var select = @"select Id,Url from article where published >= '2016-03-01' and feedid in (select id from feed where public = 1)";

            var articles = dp.GetFromSelect(select);

            var skip = true;
            var allRows = articles.Tables[0].Rows;
            foreach (DataRow dr in allRows)
            {
                try
                {
                    //7015748
                    var id = Convert.ToInt32(dr["Id"]);
                    if (id == 7242342)
                        skip = false;

                    if (skip) continue;

                    var url = dr["Url"].ToString();
                    var request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Method = "HEAD";
                    request.Headers["Accept-Encoding"] = "gzip,deflate";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    request.KeepAlive = true;
                    request.Timeout = 10000;
                    request.MaximumAutomaticRedirections = 3;
                    request.MaximumResponseHeadersLength = 1024 * 64 * 64;
                    if (request != null)
                    {
                        var response = request.GetResponse() as HttpWebResponse;
                        var headers = response.Headers;
                        var xFrameOptions = string.Empty;

                        if (response != null)
                        {
                            response.Close();
                        }

                        if (response.Headers != null)
                        {
                            xFrameOptions = response.Headers["X-Frame-Options"];
                        }
                        if (!xFrameOptions.IsNullOrEmpty())
                        {
                            if (xFrameOptions == "deny" ||
                                xFrameOptions == "SAMEORIGIN" ||
                                !xFrameOptions.Contains("rssheap"))
                            {
                                dp.GetFromSelect("update article set Flagged = 1 where Id =" + id);
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }

        [TestMethod]
        public void CheckEmailDKIM()
        {
            Mail.SendEmailWithDKIP("This is the subject", "This should be the body");
        }

        [TestMethod]
        public void CheckIfFeedIsInEnglish()
        {
            var feeds = FeedService.GetFeeds().Where(f => !f.Public).ToList();
            var notInEnglish = new List<Feed>();
            foreach (var feed in feeds)
            {
                if (!IsEnglish(feed.Name + feed.Description))
                {
                    notInEnglish.Add(feed);
                    if (notInEnglish.Count == 76)
                    {

                    }
                }
            }

            foreach (var f in notInEnglish)
            {
                f.Reviewed = true;
                FeedService.UpdateFeed(f);
            }
        }

        public bool IsEnglish(string inputstring)
        {
            Regex regex = new Regex("[A-Za-z0-9 \".,'-=+&(){}\\[\\]\\\\|!\r\n\t/’;-@%‘’’`‘®»_#—’]");
            MatchCollection matches = regex.Matches(inputstring);

            if (matches.Count.Equals(inputstring.Length))
                return true;
            else
            {
                if (inputstring.Length > 500 && (inputstring.Length - matches.Count < 5))
                    return true;
                return false;
            }
        }

        [TestMethod]
        public void ImportUserOpmls()
        {
            foreach (var file in Directory.GetFiles(@"D:\Dropbox\Projects\RSSReader\Web\OPML"))
            {
                var fileName = Path.GetFileName(file);
                var userId = string.Empty;
                foreach (char c in fileName)
                {
                    if (c == ' ')
                    {
                        break;
                    }
                    userId += c;
                }

                try
                {
                    FeedService.InsertOPML(int.Parse(userId), fileName);
                }
                catch { }
            }
        }

        private bool ImportUrl(User CurrentUser, string htmlUrl, string rssUrl, string folderName = null, bool opml = false)
        {
            if (!htmlUrl.IsNullOrEmpty() &&
                !htmlUrl.ToLower().StartsWith("http") &&
                !htmlUrl.ToLower().StartsWith("https") &&
                !htmlUrl.ToLower().StartsWith("www"))
                htmlUrl = "http://" + htmlUrl;

            try
            {
                var urls = new List<string>();
                if (rssUrl.IsNullOrEmpty() && htmlUrl.IsNullOrEmpty()) return false;
                if (!htmlUrl.IsNullOrEmpty() && !htmlUrl.StartsWith("http://") && !htmlUrl.StartsWith("https")) htmlUrl = "http://" + rssUrl;

                Feed exsFeed = null;
                if (!htmlUrl.IsNullOrEmpty())
                    exsFeed = FeedService.GetFeedBySiteUrl(htmlUrl);

                if (!rssUrl.IsNullOrEmpty())
                    exsFeed = FeedService.GetFeedByXmlUrl(rssUrl);

                if (exsFeed != null)
                {
                    var subscribe = true;
                    var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == exsFeed.Id);

                    if (userFeed == null)
                    {
                        userFeed = new UserFeed
                        {
                            FeedId = exsFeed.Id,
                            UserId = CurrentUser.Id,
                            Subscribed = subscribe,
                            Submited = true
                        };
                        userFeed.Id = UserService.InsertUserFeed(userFeed);
                        CurrentUser.MyFeeds.Add(userFeed);
                        CurrentUser.Reputation += 3;
                        UserService.UpdateUser(CurrentUser);
                    }

                    if (!folderName.IsNullOrEmpty())
                    {
                        //create folder
                        var folder = UserService.GetUserFolder(CurrentUser.Id, folderName);
                        if (folder == null)
                        {
                            folder = UserService.InsertUserFolder(CurrentUser.Id, new Folder
                            {
                                Name = folderName,
                                UserId = CurrentUser.Id
                            });
                            CurrentUser.Folders.Add(folder);
                        }
                        if (folder != null)
                        {
                            try
                            {
                                UserService.InsertUserFeedFolder(CurrentUser.Id, exsFeed.Id, folder.Id);
                            }
                            catch { }
                        }
                    }

                    return true;
                }

                if (opml)
                {
                    var rFeed = new Feed
                    {
                        Url = rssUrl,
                        SiteUrl = htmlUrl
                    };
                    var lFeed = FeedService.GetFeedBySiteUrl(htmlUrl);
                    int feedId = lFeed == null ? FeedService.InsertFeed(rFeed) : lFeed.Id;

                    var subscribe = true;
                    var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == feedId);

                    if (userFeed == null)
                    {
                        userFeed = new UserFeed
                        {
                            FeedId = feedId,
                            UserId = CurrentUser.Id,
                            Subscribed = subscribe,
                            Submited = true
                        };
                        userFeed.Id = UserService.InsertUserFeed(userFeed);
                        CurrentUser.MyFeeds.Add(userFeed);
                        CurrentUser.Reputation += 3;
                        UserService.UpdateUser(CurrentUser);
                    }

                    if (!folderName.IsNullOrEmpty())
                    {
                        //create folder
                        var folder = UserService.GetUserFolder(CurrentUser.Id, folderName);
                        if (folder == null)
                        {
                            folder = UserService.InsertUserFolder(CurrentUser.Id, new Folder
                            {
                                Name = folderName,
                                UserId = CurrentUser.Id
                            });
                            CurrentUser.Folders.Add(folder);
                        }
                        if (folder != null)
                        {
                            try
                            {
                                UserService.InsertUserFeedFolder(CurrentUser.Id, feedId, folder.Id);
                            }
                            catch { }
                        }
                    }
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        [TestMethod]
        public void SendMeAnEmail()
        {
            Mail.SendMeAnEmail("this is the subject", "this is the body");
        }

        [TestMethod]
        public void Index()
        {
            var articles = new FeedService().GetArticles();
            var feeds = new FeedService().GetFeeds();

            var lucene = new LuceneSearch();
            foreach (var article in articles)
            {
                article.Feed = feeds.Find(f => f.Id == article.FeedId);
                article.Tags = new FeedService().GetArticleTags(article.Id).Where(t => t.Approved).ToList();
                lucene.AddUpdateIndex(article);
            }
        }

        [TestMethod]
        public void GetArticles()
        {
            var articles = new FeedService().GetArticlesIndexedBefore(DateTime.Now.AddDays(1), 10);
        }

        [TestMethod]
        public void SearchIndex()
        {
            var lucene = new LuceneSearch();
            var articles = lucene.SearchDefault("c++", 1, 10);
        }

        [TestMethod]
        public void FollowUser()
        {
            int userId = 3;
            int userToFollow = 2;
            new UserService().FollowUser(userId, userToFollow);
        }
    }
}
