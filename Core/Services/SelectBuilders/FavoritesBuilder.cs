using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Services.SelectBuilders
{
    internal class FavoritesBuilder : ArticlesSelectBuilder
    {
        public override string GetWhere()
        {
            var where = " where Article.Id in " +
                         "(" +
                         "  select ArticleId from UserFavoriteArticle where UserId = " + Request.User.Id +
                         ") ";

            if (Request.IsSingleArticleRequest)
            {
                var article = new FeedService().GetArticle(Request.ArticleId);
                where += Environment.NewLine;
                where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) and LikesCount <= " + article.LikesCount;
                where += Environment.NewLine;
            }

            return where;
        }

        public override string GetJoins()
        {
            return string.Empty;
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
