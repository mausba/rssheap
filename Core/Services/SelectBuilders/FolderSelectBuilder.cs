using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.SelectBuilders
{
    public class FolderSelectBuilder : ArticlesSelectBuilder
    {
        public override string GetWhere()
        {
            var where = " where Article.FeedId in (select FeedId from UserFeedFolder where UserId = " + Request.User.Id + " and FolderId = " + Request.FolderId + ") ";

            where += " and Feed.Id not in (select FeedId from UserFeedIgnored where UserId = " + Request.User.Id + ") ";
            where += " and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = " + Request.User.Id + ") ";
            
            switch (Request.Filter)
            {
                case "week":
                    where += " and Published >= '" + DateTime.Now.AddDays(-7).ToMySQLString() + "' ";
                    break;
                case "month":
                    where += " and Published <= '" + DateTime.Now.Date.AddDays(-7).ToMySQLString() + "' ";
                    where += " and Published >= '" + DateTime.Now.Date.AddDays(-30).ToMySQLString() + "' ";
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

            if (Request.IgnoredArticleIds.Count > 0)
                where += " and Article.Id not in (" + Request.IgnoredArticleIds.ToCommaSeparetedListOfIds() + ") ";

            if (Request.User.HideVisitedArticles)
            {
                where += @" and Article.Id not in (
                            select ArticleId from UserReadArticle where UserId = " + Request.User.Id + @"
                        ) ";
            }

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
