using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.SelectBuilders
{
    public class MyFeedsSelectBuilder : ArticlesSelectBuilder
    {
        public override string GetWhere()
        {
            var myFeeds = Request.User
                                 .MyFeeds
                                 .Where(f => f.Subscribed)
                                 .Select(f => f.FeedId)
                                 .ToList();
            if (myFeeds.Count == 0)
                return "where 1 = 0";

            var where = "where Feed.Id in (" + myFeeds.ToCommaSeparetedListOfIds() + ")";

            switch (Request.Filter)
            {
                case "week":
                    where += " and Article.Published >= '" + DateTime.Now.AddDays(-7).ToMySQLString() + "' ";
                    break;
                case "month":
                    where += " and Article.Published <= '" + DateTime.Now.Date.AddDays(-7).ToMySQLString() + "' ";
                    where += " and Article.Published >= '" + DateTime.Now.Date.AddDays(-30).ToMySQLString() + "' ";
                    break;
                case "votes":

                    break;
                case "favorites":
                    where += " and Article.Id in " +
                         "(" +
                         "  select ArticleId from UserFavoriteArticle where UserId = " + Request.User.Id +
                         ") ";
                    break;
            }
            if (Request.User.HideVisitedArticles)
            {
                where += @" and Article.Id not in (
                            select ArticleId from UserReadArticle where UserId = " + Request.User.Id + @"
                        ) ";
            }

            where += " and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = " + Request.User.Id + ") ";
            where += " and Article.Flagged = 0 ";

            if (Request.IsSingleArticleRequest)
            {
                var article = new FeedService().GetArticle(Request.ArticleId);
                where += Environment.NewLine;
                where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) and LikesCount <= " + article.LikesCount;
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
