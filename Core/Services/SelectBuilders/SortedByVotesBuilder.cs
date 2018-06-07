using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Services.SelectBuilders
{
    internal class SortedByVotesBuilder : ArticlesSelectBuilder
    {
        public override string GetWhere()
        {
            var where = " where ( 1 = 1 ";

            where += " and Feed.Id not in (select FeedId from UserFeedIgnored where UserId = " + Request.User.Id + ") ";
            where += " and Article.Id not in (select ArticleId from UserArticleIgnored where UserId = " + Request.User.Id + ") ";

            if (Request.User.HideOlderThanDateTime > DateTime.MinValue)
            {
                where += " and Article.Published >= '" + Request.User.HideOlderThanDateTime.ToMySQLString() + "'";
            }

            if (Request.IgnoredArticleIds.Count > 0)
                where += " and Article.Id not in (" + Request.IgnoredArticleIds.ToCommaSeparetedListOfIds() + ") ";

            if (!Request.DoNotFilterByTags)
            {
                if (Request.User.FavoriteTagIds.Count > 0)
                {
                    where += " and Article.Id in (";
                    where += " select ArticleId from ArticleTag where ";
                    where += " ArticlePublic = 1 and ";
                    where += " TagId in (" + Request.User.FavoriteTagIds.ToCommaSeparetedListOfIds() + ")";
                    where += " )";
                }
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
            where += " and Article.Flagged = 0 ";

            if(Request.IsSingleArticleRequest)
            {
                var article = new FeedService().GetArticle(Request.ArticleId);
                where += Environment.NewLine;
                where += " and Article.Id not in ( " + SessionService.GetVisitedArticles(Request.User.Id).ToCommaSeparetedListOfIds() + " ) ";
                if(article != null)
                {
                    where += "and LikesCount <= " + article.LikesCount + " ";
                }
                where += Environment.NewLine;
            }

            return where;
        }

        public override string GetJoins()
        {
            var join = string.Empty;
            if (Request.User.HideOlderThanDateTime > DateTime.MinValue)
            {
                join += " inner join Feed Feed2 on Article.FeedId = Feed2.Id ";
            }
            return join;
        }

        public override string GetOrderBy()
        {
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
