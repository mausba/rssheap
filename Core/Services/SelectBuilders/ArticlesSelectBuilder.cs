using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Data;
using Core.Models;
using Core.Models.Requests;

namespace Core.Services.SelectBuilders
{
    public abstract class ArticlesSelectBuilder
    {
        public ArticlesRequest Request { get; set; }

        public string GetSelect(ArticlesRequest request)
        {
            Request = request;

            var select = string.Empty;
            select = "select * from (" + Environment.NewLine;

            if (request.IsSingleArticleRequest)
            {
                select += @"
                  select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Body as 'Article.Body', Article.ShortUrl as 'Article.ShortUrl', 
                  Article.Url as 'Article.Url', Article.Published as 'Article.Published', Article.Published, Article.Created as 'Article.Created',
                  Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', Article.LikesCount, Article.FavoriteCount as 'Article.FavoriteCount',
                  Article.Flagged as 'Article.Flagged', Article.FlaggedBy as 'Article.FlaggedBy',
                  Feed.Id as 'Feed.Id', Feed.Name as 'Feed.Name', Feed.Descriptions as 'Feed.Descriptions', Feed.Url as 'Feed.Url', Feed.SiteUrl as 'Feed.SiteUrl',
                  (select Votes from UserArticleVote where UserId = " + request.User.Id + @" and ArticleId = Article.Id) as Votes,
                  (select Id from UserFavoriteArticle where ArticleId = Article.Id and UserId = " + request.User.Id + @") as MyFavoriteId,
                  (select count(*) from UserFavoriteArticle where ArticleId = Article.Id) as FavoriteCount";

                if(request.AppVersion == 2 || !request.IsFromMobile)
                {
                    select = select.Replace(", Article.Body as 'Article.Body'", ", '' as 'Article.Body'");
                }
            }
            else
            {
                select += @"
                select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Published as 'Article.Published', Article.Published,
                Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', Article.LikesCount,
                0 as 'Article.Flagged', '' as 'Article.FlaggedBy',
                Article.ShortUrl as 'Article.ShortUrl',
                Feed.Id as 'Feed.Id', Feed.Name as 'Feed.Name', Feed.SiteUrl as 'Feed.SiteUrl' ";

                if (request.IncludeArticleBody)
                    select += ", Article.Body as 'Article.Body' ";

                if (request.AppVersion == 2 || !request.IsFromMobile)
                {
                    select = select.Replace(", Article.Body as 'Article.Body' ", ", '' as 'Article.Body' ");
                }
            }

            select += @" from Article

            inner join Feed on Article.FeedId = Feed.Id ";

            select += GetJoins() + Environment.NewLine;
            select += GetWhere() + Environment.NewLine;

            if (request.User.HideOlderThanDateTime > DateTime.MinValue)
            {
                select += " and Article.Published >= '" + request.User.HideOlderThanDateTime.ToMySQLString() + "'";
            }

            select += " group by Article.Name " + Environment.NewLine;

            select += Environment.NewLine + ") as T " + Environment.NewLine;

            select += @" order by " + GetOrderBy();

            select += GetLimit();


            return select;
        }

        public virtual string GetLimit()
        {
            var limit = string.Empty;
            if (!Request.IsSingleArticleRequest)
            {
                if (Request.Page <= 1)
                {
                    limit += @" limit " + Request.PageSize + Environment.NewLine;
                }
                else
                {
                    limit += @" limit " + (Request.Page - 1) * Request.PageSize + "," + Request.PageSize + Environment.NewLine;
                }
            }
            else
            {
                if (Request.Page <= 1)
                {
                    limit += @" limit " + Request.PageSize + Environment.NewLine;
                }
                else
                {
                    limit += @" limit " + (Request.Page - 1) + "," + Request.PageSize;
                }
            }
            return limit;
        }

        public virtual Dictionary<string, object> GetParameters()
        {
            return null;
        }

        //if you override this method make sure to check 
        //for requests to single item because LikesCount is used 
        //for pagination on a requests for single article
        public virtual string GetOrderBy()
        {
            return " LikesCount desc ";
        }

        public virtual string GetJoins()
        {
            return string.Empty;
        }

        public abstract string GetWhere();
    }
}
