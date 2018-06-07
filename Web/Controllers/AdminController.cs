using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Models;
using Core.Services;
using Web.Code.ActionFilters;
using Web.Code.Attributes;
using Core.Caching;

namespace MvcWeb.Controllers
{
    [AdminAuthorize]
    public class AdminController : _BaseController
    {
        public ActionResult Index()
        {
            var categories = FeedService.GetTags().Where(c => c.ArticlesCount > 2).ToList();
            return View(categories);
        }

        [NonSSL]
        public ActionResult Feeds()
        {
            var feeds = FeedService.GetFeedsNotPublicAndReviewed().OrderByDescending(f => f.TotalLikes).ToList();
            return View(feeds);
        }

        [HttpPost]
        public ActionResult Feeds(int feedId, string action)
        {
            var feed = FeedService.GetFeed(feedId);
            switch (action)
            {
                case "reviewed" : feed.Reviewed = true; break;
                case "public": feed.Public = true; break;

            }

            if (action == "delete")
            {
                FeedService.DeleteFeed(feedId);
            }
            else
            {
                FeedService.UpdateFeed(feed);
            }

            if(action == "public")
            {
                var articles = FeedService.GetArticles(feed.Id);
                foreach(var article in articles)
                {
                    article.Feed = feed;
                    article.Tags = FeedService.GetArticleTags(article.Id);
                    Redis.AddArticle(article);
                }
            }

            return RedirectToAction("Feeds");
        }

        [NonSSL]
        public ActionResult Articles()
        {
            var article = FeedService.GetArticleWithNotApprovedTags();
            if (article == null) article = new Article();

            return View(article);
        }

        public ActionResult synonim(int id, int parentId)
        {
            var category = FeedService.GetTag(id);
            var parentCategory = FeedService.GetTag(parentId);

            if (category != null && parentCategory != null)
            {
                category.SynonimTagId = parentCategory.Id;
                FeedService.UpdateTagAndArticleCount(category);
            }
            return RedirectToAction("Index");
        }

        public ActionResult similarto(int id, int categoryId)
        {
            var category = FeedService.GetTag(id);
            var parentCategory = FeedService.GetTag(categoryId);

            if (category != null && parentCategory != null)
            {
                category.SimilarTagIds.Add(parentCategory.Id);
                FeedService.UpdateTagAndArticleCount(category);
            }
            return RedirectToAction("Index");
        }
    }
}
