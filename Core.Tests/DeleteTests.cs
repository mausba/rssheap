using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Services;
using Core.Data;
using System.Data;
using System.Net;
using System.Threading.Tasks;

namespace Core.Tests
{
    [TestClass]
    public class DeleteTests
    {
        public FeedService FeedService = new FeedService();
        public DataProvider dp = new DataProvider();

        [TestMethod]
        public void DeleteTag()
        {
            var tagNamesds = new DataProvider().GetFromSelect("select * from Tag where Active = 0");
            var dp = new DataProvider();
            foreach (DataRow tag in tagNamesds.Tables[0].Rows)
            {
                var tagId = Convert.ToInt32(tag["Id"]);
                var tagName = tag["Name"].ToString();

                var tagsds = dp.GetFromSelect("select * from Tag where Name = '" + tagName + "'");
                if (tagsds.Tables[0].Rows.Count == 0) throw new Exception("Unknown tag");


                var users = dp.GetFromSelect("select * from user");
                foreach (DataRow drUser in users.Tables[0].Rows)
                {
                    var id = Convert.ToInt32(drUser["id"]);
                    if (drUser["tagids"] == DBNull.Value) continue;
                    var tags = (string)drUser["tagids"];
                    if (tags.IsNullOrEmpty()) continue;

                    var tagsList = tags.Split(',').ToList();
                    if (!tagsList.Contains(tagId.ToString())) continue;
                    tagsList.Remove(tagId.ToString());

                    dp.GetFromSelect("update User set TagIds = '" + string.Join(",", tagsList) + "' where id = " + id);
                }

                dp.GetFromSelect("delete from ArticleTag where TagId = " + tagId);
                dp.GetFromSelect("delete from Tag where Id = " + tagId);
            }

            
        }

        [TestMethod]
        public void DeleteDuplicateFeeds()
        {
            var feeds = FeedService.GetFeeds()
                                   .Where(f => !f.SiteUrl.IsNullOrEmpty() &&
                                                !f.SiteUrl.Contains("http://www.dotnettips.info") &&
                                                !f.SiteUrl.Contains("http://www.vitnew.com/"))
                                   .OrderBy(f => f.Id)
                                   .ToList();
            foreach (var feed in feeds.ToList())
            {
                //any duplicates?
                var duplicates = feeds.Where(f => f.Id != feed.Id && f.SiteUrl == feed.SiteUrl).ToList();
                if (duplicates.Any())
                {
                    foreach (var duplicate in duplicates)
                    {
                        try
                        {
                            //get votes
                            var votes = dp.GetFromSelect("select * from UserArticleVote where ArticleId in (select Id from Article where FeedId = " + duplicate.Id + ")");
                            if (votes.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow dr in votes.Tables[0].Rows)
                                {
                                    var oldArticle = dp.GetFromSelect("select * from Article where Id = " + dr["ArticleId"].ToString()).ToArticles().FirstOrDefault();
                                    var newArticle = dp.GetFromSelect("select * from Article where FeedId = " + feed.Id + " and Name = '" + oldArticle.Name + "'").ToArticles().FirstOrDefault();

                                    dp.GetFromSelect("update UserArticleVote set ArticleId = " + newArticle.Id + " where ArticleId = " + oldArticle.Id);
                                }
                            }

                            var userRead = dp.GetFromSelect("select * from UserReadArticle where ArticleId in (select Id from Article where FeedId = " + duplicate.Id + ")");
                            if (userRead.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow dr in userRead.Tables[0].Rows)
                                {
                                    var oldArticle = dp.GetFromSelect("select * from Article where Id = " + dr["ArticleId"].ToString()).ToArticles().FirstOrDefault();
                                    var newArticle = dp.GetFromSelect("select * from Article where FeedId = " + feed.Id + " and Name = @name", new Dictionary<string, object> { { "name", oldArticle.Name } }).ToArticles().FirstOrDefault();

                                    if (newArticle != null)
                                    {
                                        dp.GetFromSelect("update UserReadArticle set ArticleId = " + newArticle.Id + " where ArticleId = " + oldArticle.Id);
                                    }
                                    else
                                    {
                                        dp.GetFromSelect("delete from UserReadArticle where ArticleId = " + oldArticle.Id);
                                    }
                                }
                            }

                            var ignoredArticles = dp.GetFromSelect("select * from UserArticleIgnored where ArticleId in (select Id from Article where FeedId = " + duplicate.Id + ")");
                            if (ignoredArticles.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow dr in ignoredArticles.Tables[0].Rows)
                                {
                                    var oldArticle = dp.GetFromSelect("select * from Article where Id = " + dr["ArticleId"].ToString()).ToArticles().FirstOrDefault();
                                    var newArticle = dp.GetFromSelect("select * from Article where FeedId = " + feed.Id + " and Name = '" + oldArticle.Name + "'").ToArticles().FirstOrDefault();

                                    if (newArticle != null)
                                    {
                                        dp.GetFromSelect("update UserArticleIgnored set ArticleId = " + newArticle.Id + " where ArticleId = " + oldArticle.Id);
                                    }
                                    else
                                    {
                                        dp.GetFromSelect("delete from UserArticleIgnored where ArticleId = " + oldArticle.Id);
                                    }
                                }
                            }
                        }
                        catch
                        { }

                        if (duplicate.Public && !feed.Public)
                        {
                        }


                        string select = @"
                        update UserFeed set FeedId = @newFeedId where FeedId = @feedId;
                        delete from UserFeedFolder where FeedId = @newFeedId;
                        update UserFeedFolder set FeedId = @newFeedId where FeedId = @feedId;

	                    delete from UserArticleVote where ArticleId in (select Id from Article where FeedId = @feedId) and Id > 0;
	                    delete from UserFavoriteArticle where ArticleId in (select Id from Article where FeedId = @feedId) and Id > 0;
	                    delete from UserReadArticle where ArticleId in (select Id from Article where FeedId = @feedId) and Id > 0;
	
                        delete from UserFeedIgnored where FeedId = @feedId and Id > 0;
	                    delete from ArticleTag where ArticleId in (select Id from Article where FeedId = @feedId) and Id > 0;
	                    delete from Article where FeedId = @feedId and id > 0;
	                    delete from UserFeed where FeedId = @feedId;
	                    delete from Feed where Id = @feedId and Id > 0;";

                        dp.GetFromSelect(select, new Dictionary<string, object>
                            {
                                {"feedId", duplicate.Id },
                                {"newFeedId", feed.Id}
                            });

                        DeleteDuplicateFeeds();
                        return;
                    }
                    //delete it
                }
            }
        }

        [TestMethod]
        public void DeleteFeedArticles()
        {
            var feedId = 3;
            var articles = new FeedService().GetArticlesWithoutBody(feedId);
            articles.ForEach(a => new FeedService().DeleteArticle(a.Id));
        }

        [TestMethod]
        public void DeleteTest()
        {
            
            var s = dp.GetFromSelect("select * from Feed where Id not in (select FeedId from article)").ToFeeds();
            foreach (var feed in s)
           { 
                var request = (HttpWebRequest)WebRequest.Create(feed.Url);
                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        FeedService.DeleteFeed(feed.Id);
                    }
                }
                catch (WebException we)
                {
                    if (we.Status == WebExceptionStatus.NameResolutionFailure || (we.Response != null && ((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.NotFound))
                    {
                        FeedService.DeleteFeed(feed.Id);
                    }
                }
                if (response != null)
                    response.Close();
            }
        }
    }
}
