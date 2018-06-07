using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using Core.Services;
using Core.Models.Requests;
using Core.Data;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using Core.Caching;

namespace Core.Tests
{
    [TestClass]
    public class RedisTests
    {
        [TestMethod]
        public void TestGetAdd()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            var db = redis.GetDatabase();

            //var server = redis.GetServer(redis.GetEndPoints().First());
            //foreach (var key in server.Keys())
            //{
            //    db.KeyDelete(key);
            //}

            var weekArticles = Redis.GetArticles(27, "week");

            var sTemp = @"select * from (

                select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Name, Article.Published as 'Article.Published', Article.Published,
                Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', Article.LikesCount,
                Article.Flagged as 'Article.Flagged', Article.FlaggedBy as 'Article.FlaggedBy',
                Article.ShortUrl as 'Article.ShortUrl',
                Feed.Id as 'Feed.Id', Feed.Name as 'Feed.Name', Feed.SiteUrl as 'Feed.SiteUrl'  from Article

                inner join Feed on Article.FeedId = Feed.Id 
                    where (Published >= '2015-08-04 00:00:00'  and Article.Flagged = 0  and Published <= '2015-08-11 23:59:00'  and Feed.Id not in (select FeedId from UserFeedIgnored where UserId = 25)  and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = 25)  and Article.Id in ( select ArticleId from ArticleTag where  ArticlePublic = 1 and  TagId in (5,9,24,139,153,185,200,553,1004,1992,2221,2222,2231,2237,2251,2332,2358,2394) and ArticlePublished >= '2015-08-04 00:00:00'  and ArticlePublished <= '2015-08-11 23:59:00' ) and Article.Id not in ( select ArticleId from ArticleTag where ArticlePublic = 1 and TagId in (830) and ArticlePublished >= '2015-08-04 00:00:00'  and ArticlePublished <= '2015-08-11 00:00:00' ) and ( (Feed.Public = 1 ))
                )

                ) as T 
                    order by  LikesCount desc, Name
                ";

            var dsSelect = new DataProvider().GetFromSelect(sTemp).ToArticlesWithAssObjects();
            var missingArticles = dsSelect.Where(a => !weekArticles.Select(ar => ar.Id).Contains(a.Id)).ToList();

            var usersSelect = "select * from User";
            var users = new DataProvider().GetFromSelect(usersSelect).ToUsers();
            foreach(var user in users)
            {
                Redis.AddUser(user);
                foreach (var tagId in user.FavoriteTagIds)
                {
                    Redis.AddUserTag(user, tagId, user.IgnoredTagIds);
                }
            }

            var select = @"select Id, FeedId, Name, Body, Url, ViewsCount, LikesCount, FavoriteCount, 
                            Published, ShortUrl from Article where 
                            FeedId in (select Id from Feed where Public = 1) and Flagged = 0 and
                            Published >= '" + DateTime.Now.Date.AddMonths(-1).ToMySQLString() + "'";
            var articles = new DataProvider().GetFromSelect(select, null).ToArticles().OrderBy(a => a.Published).ToList();

            var feeds = new FeedService().GetFeeds(articles.Select(a => a.FeedId).ToList());

            //add articles to hashset
            foreach (var article in articles)
            {
                article.Feed = feeds.Find(f => f.Id == article.FeedId);
                article.Tags = new FeedService().GetTagsForArticle(article.Id);

                Redis.AddArticle(article);
            }
        }
    }
}
