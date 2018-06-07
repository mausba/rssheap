using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Services.SelectBuilders
{
    internal class FeedBuilder : ArticlesSelectBuilder
    {
        public override string GetWhere()
        {
            var where = " where Feed.Id = " + Request.FeedId;
            where += " and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = " + Request.User.Id + ") ";

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

            if(Request.IsSingleArticleRequest)
            {
                var article = new FeedService().GetArticle(Request.ArticleId);
                where += Environment.NewLine;
                if (Request.Filter != "votes")
                    where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) and Article.Published <= '" + article.Published.ToMySQLString() + "' ";
                else if (Request.Filter == "all")
                    where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) and Article.Published <= '" + article.Published.ToMySQLString() + "' ";
                else
                    where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) and Article.LikesCount <= " + article.LikesCount + " ";

                where += Environment.NewLine;
            }

            return where;
        }

        public override string GetOrderBy()
        {
            if (Request.Filter != "votes")
                return " Published desc ";

            if (Request.Filter == "all")
                return " Published desc ";

            return " LikesCount desc ";
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
