using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Services.SelectBuilders
{
    public class PublishedAfterBuilder : ArticlesSelectBuilder
    {
        public DateTime PublishedAfter { get; set; }
        public DateTime PublishedBefore { get; set; }

        public PublishedAfterBuilder(DateTime publishedAfter, DateTime publishedBefore)
        {
            PublishedAfter = publishedAfter;
            PublishedBefore = publishedBefore;
        }

        public override string GetOrderBy()
        {
            return " LikesCount desc, Published ";
        }

        public override string GetJoins()
        {
            return string.Empty;
        }

        public override string GetWhere()
        {
            var where = " where (";

            where += "Published >= '" + PublishedBefore.Date.ToMySQLString() + "' ";
            where += " and Article.Flagged = 0 ";
            if (PublishedAfter > DateTime.MinValue)
                where += " and Published <= '" + PublishedAfter.Date.AddDays(1).AddMinutes(-1).ToMySQLString() + "' ";

            where += " and Feed.Id not in (select FeedId from UserFeedIgnored where UserId = " + Request.User.Id + ") ";
            where += " and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = " + Request.User.Id + ") ";

            if (Request.IgnoredArticleIds.Count > 0)
                where += " and Article.Id not in (" + Request.IgnoredArticleIds.ToCommaSeparetedListOfIds() + ") ";

            if (!Request.DoNotFilterByTags)
            {
                where += " and Article.Id in (";
                where += " select ArticleId from ArticleTag where ";
                where += " ArticlePublic = 1 ";

                if (Request.User.FavoriteTagIds.Count > 0)
                    where += " and TagId in (" + Request.User.FavoriteTagIds.ToCommaSeparetedListOfIds() + ")";

                where += " and ArticlePublished >= '" + PublishedBefore.Date.ToMySQLString() + "' ";
                if (PublishedAfter > DateTime.MinValue)
                    where += " and ArticlePublished <= '" + PublishedAfter.Date.AddDays(1).AddMinutes(-1).ToMySQLString() + "' ";
                where += ")";
            }

            where += " and (";
            where += " (";
            where += "Feed.Public = 1 ";
            where += ")";
            if (Request.User.IgnoredFeedIds.Any())
            {
                where += " and Article.FeedId not in (" + Request.User.IgnoredFeedIds.ToCommaSeparetedListOfIds() + ")";
            }

            where += ")" + Environment.NewLine;
            if (Request.User.HideVisitedArticles)
            {
                where += @" and Article.Id not in (
                            select ArticleId from UserReadArticle where UserId = " + Request.User.Id + @"
                        ) ";
            }

            where += ")";

            if (Request.IsSingleArticleRequest && Request.ArticleId > 0)
            {
                var article = new FeedService().GetArticle(Request.ArticleId);
                where += Environment.NewLine;
                where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) ";
                if(article != null)
                {
                    where += "and Article.LikesCount <= " + article.LikesCount + " ";
                }
                where += Environment.NewLine;
            }
            
            return where;
        }

        public override string GetLimit()
        {
            if (Request.IsFromMobile)
                return base.GetLimit();

            if (Request.IsSingleArticleRequest)
                return " limit 0,1 ";

            return base.GetLimit();
        }
    }
}
