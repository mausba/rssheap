using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using System.Data;
using Core.Enums;
using Core.Services;
using Core.Models.Requests;
using Core.Services.SelectBuilders;
using Core.Utilities;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Core.Data
{
    public class DataProvider
    {
        private static readonly string connStr = "Server=localhost;Database=rss_com_db;Uid=root;Pwd=palestine";

        static DataProvider()
        {
       //    connStr = Configuration.GetConnectionString();
        }

        private void ExecuteStoredProcedure(string storedProcedure, Dictionary<string, object> parameters = null)
        {
            GetDataSet(storedProcedure, parameters, CommandType.StoredProcedure);
        }

        private DataSet Get(string storedProcedure, Dictionary<string, object> parameters = null)
        {
            return GetDataSet(storedProcedure, parameters, CommandType.StoredProcedure);
        }

        public DataSet GetFromSelect(string select, Dictionary<string, object> parameters = null)
        {
            return GetDataSet(select, parameters, CommandType.Text);
        }

        public DataSet GetDataSet(string storedProcedure, Dictionary<string, object> parameters = null)
        {
            return GetDataSet(storedProcedure, parameters, CommandType.StoredProcedure);
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private DataSet GetDataSet(string storedProcedure, Dictionary<string, object> parameters, CommandType commandType)
        {
            var ds = new DataSet();
            var started = DateTime.Now;

            var conn = new MySqlConnection(connStr);
            var cmd = new MySqlCommand
            {
                Connection = conn,
                CommandTimeout = 360,
                CommandText = storedProcedure,
                CommandType = commandType
            };

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var paramName = p.Key;
                    if (commandType == CommandType.StoredProcedure)
                    {
                        paramName += "Param";
                    }
                    cmd.Parameters.AddWithValue(paramName, p.Value);
                    cmd.Parameters[paramName].Direction = ParameterDirection.Input;
                }
            }

            var da = new MySqlDataAdapter
            {
                SelectCommand = cmd
            };
            conn.Open();
            da.Fill(ds);
            conn.Close();

            var lasted = (DateTime.Now - started).TotalSeconds;
            if (lasted > 20)
            {
                IISTaskManager.Run(() =>
                {
                    var subject = "took more than 20 seconds";
                    var body = lasted + "<br/>" + storedProcedure + "<br/>";

                    if (ds.Tables.Count > 0)
                        body += ds.Tables[0].Rows.Count + " returned <br/>";

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            body += param.Key + " : " + param.Value.ToString() + "<br/>";
                        }
                    }
                    body += "<br/><br/>" + Environment.GetCommandLineArgs()[0] + "<br/><br/>" + Environment.StackTrace;


                    Mail.SendMeAnEmail(subject, body);
                });
            }

            return ds;

        }

        public User GetUser(string userName, string password)
        {
            return Get("GetUserByUserNameAndPassword",
                new Dictionary<string, object>
                {
                    { "userName", userName },
                    { "password", password }
                }).ToUsers().FirstOrDefault();
        }

        internal int InsertUser(User user)
        {
            user.GUID = Guid.NewGuid().ToString("N");
            if (user.Email == null)
                user.Email = string.Empty;
            if (user.LastName == null)
                user.LastName = string.Empty;

            return Get("InsertUser",
                new Dictionary<string, object>
                {
                    { "userName", user.UserName },
                    { "salt", user.Salt },
                    { "password", user.Password },
                    { "date", DateTime.Now },
                    { "remoteid", user.RemoteId },
                    { "loginprovider", Enum.GetName(typeof(LoginProvider), user.LoginProvider) },
                    { "firstname", user.FirstName },
                    { "lastname", user.LastName },
                    { "email", user.Email },
                    { "guid", user.GUID },
                }).ToIdentity();
        }

        internal int InsertFeed(Feed feed)
        {
            return Get("InsertFeed",
                new Dictionary<string, object>
                {
                    { "url", feed.Url.ToLower() },
                    { "name", feed.Name },
                    { "description", feed.Description },
                    { "siteUrl", feed.SiteUrl },
                    { "author", feed.Author },
                    { "created", feed.Created },
                    { "public", feed.Public }
                }).ToIdentity();
        }


        /// <summary>
        /// Updates name, description, url and updated values of feed object
        /// </summary>
        /// <param name="feed"></param>
        internal void UpdateFeed(Feed feed)
        {
            ExecuteStoredProcedure("UpdateFeed",
                new Dictionary<string, object>
                {
                    { "id", feed.Id },
                    { "name", feed.Name },
                    { "description", feed.Description },
                    { "url", feed.Url },
                    { "siteUrl", feed.SiteUrl },
                    { "updated", feed.Updated },
                    { "author", feed.Author },
                    { "public", feed.Public },
                    { "reviewed", feed.Reviewed }
                });
        }

        internal Feed GetFeed(int id)
        {
            return Get("GetFeedById",
                new Dictionary<string, object>
                {
                    {"id", id}})
                .ToFeeds().FirstOrDefault();
        }

        internal List<Feed> GetFeeds()
        {
            return Get("GetFeeds")
                  .ToFeeds();
        }

        internal List<Article> GetArticlesWithoutBody(int feedId)
        {
            return Get("GetArticlesByFeedId",
                new Dictionary<string, object>
                {
                    {"feedId", feedId}})
                .ToArticles();
        }

        internal List<Article> GetArticles()
        {
            return Get("GetArticles").ToArticles();
        }

        internal Article GetArticle(int id)
        {
            return Get("GetArticleById",
                new Dictionary<string, object>
                {
                    {"id", id}})
                .ToArticles().FirstOrDefault();
        }

        internal void UpdateArticle(Article article)
        {
            var update = @"update Article set Name = @name, 
						                      Body = @body, 
						                      Url = @url, 
						                      ViewsCount = @viewsCount, 
						                      LikesCount = @likesCount,
						                      FavoriteCount = @favoriteCount,
						                      Published = @published,
                                              Flagged = @flagged,
                                              FlaggedBy = @flaggedBy
	                       where Id = @id;";

            GetFromSelect(update, new Dictionary<string, object>
                {
                    { "id", article.Id },
                    { "name", article.Name },
                    { "body", article.Body },
                    { "url", article.Url },
                    { "viewsCount", article.ViewsCount },
                    { "likesCount", article.LikesCount },
                    { "favoriteCount", article.FavoriteCount },
                    { "published", article.Published },
                    { "flagged", article.Flagged },
                    { "flaggedBy", article.FlaggedBy.ToCommaSeparetedListOfIds() },
                });
        }

        internal void UpdateUserFeed(UserFeed userFeed)
        {
            ExecuteStoredProcedure("UpdateUserFeed", new Dictionary<string, object>
                {
                    { "id", userFeed.Id },
                    { "feedid", userFeed.FeedId },
                    { "userid", userFeed.UserId },
                    { "submited", userFeed.Submited },
                    { "subscribed", userFeed.Subscribed },
                    { "ignored", userFeed.Ignored }
                });
        }

        internal void DeleteFeed(int id)
        {
            ExecuteStoredProcedure("_DeleteFeed", new Dictionary<string, object>
                {
                    {"feedId", id}});
        }

        internal void DeleteUser(int userId)
        {
            ExecuteStoredProcedure("_DeleteUser", new Dictionary<string, object>
                {
                    {"userId", userId}});
        }

        internal void DeleteArticle(int id)
        {
            ExecuteStoredProcedure("DeleteArticle", new Dictionary<string, object>
                {
                    {"id", id}});
        }

        internal int InsertLog(string error, string source)
        {
            return Get("InsertLog", new Dictionary<string, object>
                {
                    { "date", DateTime.Now },
                    { "source", source },
                    { "error", error }
                }).ToIdentity();
        }

        internal int InsertArticle(Article article)
        {
            if (!article.Flagged)
            {
                //check if we already have an article with the same name
                var existing = Get("GetArticleByName", new Dictionary<string, object>
                {
                    { "name", article.Name },
                    { "url", article.Url },
                    { "published", DateTime.Now.AddMonths(-1) }
                }).ToArticles()
                .FirstOrDefault();
                if (existing != null)
                {
                    article.Flagged = true;
                }
            }

            article.ShortUrl = article.GetShortUrl();
            var newId = Get("InsertArticle",
                new Dictionary<string, object>
                {
                    { "feedId", article.FeedId },
                    { "name", article.Name },
                    { "body", article.Body },
                    { "url", article.Url },
                    { "published", article.Published },
                    { "created", DateTime.Now },
                    { "likesCount", article.LikesCount },
                    { "shortUrl", article.ShortUrl },
                    { "flagged", article.Flagged }
                }).ToIdentity();
            article.Id = newId;
            foreach (var tag in article.Tags.Distinct())
            {
                InsertArticleTag(article, tag);
            }
            return newId;
        }

        internal int InsertUserFeed(UserFeed userFeed)
        {
            return Get("InsertUserFeed",
                new Dictionary<string, object>
                {
                    { "userid", userFeed.UserId },
                    { "feedid", userFeed.FeedId },
                    { "submited", userFeed.Submited },
                    { "subscribed", userFeed.Subscribed },
                    { "ignored", userFeed.Ignored }
                }).ToIdentity();
        }

        internal int InsertArticleVote(Vote newVote)
        {
            return Get("InsertArticleVote",
                new Dictionary<string, object>
                {
                    { "userId", newVote.UserId },
                    { "articleId", newVote.ArticleId },
                    { "votes", newVote.Votes }
                }).ToIdentity();
        }

        internal int InsertTag(Tag tag)
        {
            return Get("InsertTag",
                new Dictionary<string, object>
                {
                    { "name", tag.Name }
                }).ToIdentity();
        }

        internal int InsertFavoriteArticle(int userId, int articleId)
        {
            try
            {
                return Get("InsertFavoriteArticle",
                    new Dictionary<string, object>
                {
                    { "userId", userId },
                    { "articleId", articleId }
                }).ToIdentity();
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("duplicate"))
                    return 0;
                throw;
            }
        }

        public void DeleteFavoriteArticle(int userId, int articleId)
        {
            ExecuteStoredProcedure("DeleteFavoriteArticle", new Dictionary<string, object>
            {
                { "userId", userId },
                { "articleId", articleId }
            });
        }

        internal void IncreaseFeedFollowers(int feedId)
        {
            ExecuteStoredProcedure("IncreaseFeedFollowers", new Dictionary<string, object>
                {
                    {"id", feedId}});
        }

        internal User GetUser(int id)
        {
            return Get("GetUserById", new Dictionary<string, object>
                {
                    {"id", id}})
                .ToUsers().FirstOrDefault();
        }

        internal void UpdateUser(User user)
        {
            var update = @"update User set User.Email = @email,
					            User.FirstName = @firstName,
					            User.LastName = @lastname,
					            User.Summary = @summary,
					            User.Following = @following,
					            User.Followers = @followers,
				                User.TagIds = @tagids,
					            User.IgnoredTagIds = @ignoredtagids,
					            User.FollowingUserIds = @followingUserIds,
					            User.ImageUrl = @imageUrl,
					            User.Reputation = @reputation,
					            User.HideVisitedArticles = @hidevisitedarticles,
					            User.Subscribed = @subscribed,
                                User.HideOlderThan = @hideolderthan,
                                User.SharedOnTwitter = @sharedontwitter,
                                User.SharedOnFacebook = @sharedonfacebook
	                        where User.Id = @id;";

            GetFromSelect(update,
                new Dictionary<string, object>
                {
                    { "id", user.Id },
                    { "email", user.Email},
                    { "firstName", user.FirstName },
                    { "followers", user.Followers },
                    { "following", user.FollowingUserIds.Count },
                    { "lastName", user.LastName },
                    { "summary", user.Summary },
                    { "userName", user.UserName },
                    { "imageUrl",user.ProfilePhoto},
                    { "followingUserIds", user.FollowingUserIds.ToCommaSeparetedListOfIds() },
                    { "tagids", user.FavoriteTagIds.ToCommaSeparetedListOfIds() },
                    { "ignoredtagids", user.IgnoredTagIds.ToCommaSeparetedListOfIds() },
                    { "reputation", user.Reputation },
                    { "hidevisitedarticles", user.HideVisitedArticles },
                    { "subscribed", user.Subscribed },
                    { "hideolderthan", user.HideOlderThan },
                    { "sharedontwitter", user.SharedOnTwitter },
                    { "sharedonfacebook", user.SharedOnFacebook }
                });
        }

        internal void FlagArticle(int articleId)
        {
            GetFromSelect("update article set Flagged = 1 where Id =" + articleId);
        }

        internal void IncreaseArticleCommentsCount(int articleId)
        {
            ExecuteStoredProcedure("IncreaseArticleCommentsCount", new Dictionary<string, object>
                {
                    {"id", articleId}});
        }

        internal List<Feed> GetFeeds(List<int> ids)
        {
            if (ids.Count == 0) return new List<Feed>();
            return Get("GetFeedsByIds", new Dictionary<string, object>
            {
                {"ids", ids.RemoveDuplicates().ToCommaSeparetedListOfIdsWithoutCommas()}
            })
            .ToFeeds();
        }

        internal List<Article> GetTaggedArticles(int userId)
        {
            var select =
            @"select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Body as 'Article.Body', Article.Published as 'Article.Published', 
            Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', 
            Article.ShortUrl as 'Article.ShortUrl',
            Feed.Public as 'Feed.Public', Feed.Id as 'Feed.Id', Feed.SiteUrl as 'Feed.SiteUrl', Feed.Name as 'Feed.Name'
            from Article

            inner join Feed on Article.FeedId = Feed.Id

            where Article.Id in (select ArticleId from ArticleTag where SubmittedBy = " + userId + ")";
            return GetFromSelect(select).ToArticlesWithAssObjects();
        }

        internal List<Article> GetIgnoredArticles(int userId)
        {
            var select =
            @"select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Body as 'Article.Body', Article.Published as 'Article.Published', 
            Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', 
            Article.ShortUrl as 'Article.ShortUrl',
            Feed.Public as 'Feed.Public', Feed.Id as 'Feed.Id', Feed.SiteUrl as 'Feed.SiteUrl', Feed.Name as 'Feed.Name'
            from Article

            inner join Feed on Article.FeedId = Feed.Id

            where Article.Id in (select ArticleId from UserArticleIgnored where UserId = " + userId + ")";
            return ToArticlesWithTags(GetFromSelect(select).ToArticlesWithAssObjects());
        }

        private List<Article> ToArticlesWithTags(List<Article> articles)
        {
            var articleIds = articles.Select(a => a.Id).ToList();
            var tags = GetTagsForArticles(articleIds);

            foreach (var article in articles)
            {
                article.Tags = tags.FindAll(t => t.ArticleId == article.Id)
                                   .Select(t => new Tag
                                   {
                                       Name = t.TagName
                                   }).ToList();
            }
            return articles;
        }

        internal Article GetArticleWithAssObjects(int id, User user)
        {
            var select =
            @"select Article.Id as 'Article.Id', Article.Name as 'Article.Name', '' as 'Article.Body', Article.ShortUrl as 'Article.ShortUrl', 
              Article.Url as 'Article.Url', Article.Published as 'Article.Published', Article.Created as 'Article.Created',
              Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', Article.FavoriteCount as 'Article.FavoriteCount',
              0 as 'Article.Flagged', '' as 'Article.FlaggedBy',
              Feed.Id as 'Feed.Id', Feed.Name as 'Feed.Name', Feed.Descriptions as 'Feed.Descriptions', Feed.Url as 'Feed.Url', Feed.SiteUrl as 'Feed.SiteUrl',
              (select Votes from UserArticleVote where UserId = " + user.Id + " and ArticleId = " + id + @") as Votes,
              (select Id from UserFavoriteArticle where ArticleId = " + id + " and UserId = " + user.Id + @") as MyFavoriteId,
              (select count(*) from UserFavoriteArticle where ArticleId = " + id + @") as FavoriteCount
               from Article
                   inner join Feed on Article.FeedId = Feed.Id 
               where Article.Id = " + id;
            return GetFromSelect(select).ToArticlesWithAssObjects().FirstOrDefault();
        }

        internal List<Article> GetArticlesIndexedBefore(DateTime dateTime, int count)
        {
            var select =
            @"select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Body as 'Article.Body', Article.Published as 'Article.Published', 
            Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', 
            Article.ShortUrl as 'Article.ShortUrl',
            Feed.Public as 'Feed.Public', Feed.Id as 'Feed.Id', Feed.SiteUrl as 'Feed.SiteUrl', Feed.Name as 'Feed.Name', GROUP_CONCAT(Tag.Name) as 'Tag.Name' 
            from Article

            inner join Feed on Article.FeedId = Feed.Id
            left outer join ArticleTag on ArticleTag.ArticleId = Article.Id
            left outer join Tag on ArticleTag.TagId = Tag.Id

            WHERE Indexed <= '" + dateTime.ToMySQLString() + @"'
            AND Feed.Public = 1
            GROUP BY Article.Id 
            LIMIT 0," + count;
            return GetFromSelect(select).ToArticlesWithAssObjects();
        }

        internal List<Article> GetArticles(ArticlesRequest request)
        {
            if (!request.SearchQuery.IsNullOrEmpty())
            {
                try
                {
                    return new LuceneSearch().SearchDefault(request.SearchQuery, request.Page, request.PageSize).ToList();
                }
                catch (Exception ex)
                {
                    Mail.SendMeAnEmailEvery3mins("Failed to search with lucene", "query: " + request.SearchQuery + " ex: " + ex.ToString());
                }
            }

            var selectBuilder = GetBuilder(request);
            if (selectBuilder == null) return new List<Article>();

            if (request.ArticleId > 0)
                SessionService.AddVisitedArticle(request.User.Id, request.ArticleId);

            var articles = GetFromSelect(selectBuilder.GetSelect(request), selectBuilder.GetParameters())
                .ToArticlesWithAssObjects();

            if (!request.Untaged)
            {
                return ToArticlesWithTags(articles);
            }

            return articles;
        }

        public List<ArticleTag> GetTagsForArticles(List<int> articleIds)
        {
            if (articleIds.Count == 0) return new List<ArticleTag>();
            var select = @"select Tag.Name as TagName, ArticleTag.ArticleId as ArticleId
                          from Tag
                          left outer join ArticleTag on Tag.Id = ArticleTag.TagId
                          where ArticleTag.ArticleId in (" + articleIds.ToCommaSeparetedListOfIds() + ")";
            return GetFromSelect(select).ToArticleTags();
        }

        private static ArticlesSelectBuilder GetBuilder(ArticlesRequest request)
        {
            if (request.Week || request.Month)
            {
                var after = DateTime.Now;
                var before = DateTime.Now;
                if (request.Week)
                    after = after.AddDays(-7);
                if (request.Month)
                {
                    before = after.AddDays(-30);
                    after = before.AddDays(-7);
                }

                return new PublishedAfterBuilder(before, after);
            }

            if (request.FolderId > 0)
                return new FolderSelectBuilder();

            if (request.MyFeeds)
                return new MyFeedsSelectBuilder();

            if (request.Votes)
                return new SortedByVotesBuilder();

            if (request.Favorites)
                return new FavoritesBuilder();

            if (request.FeedId > 0)
                return new FeedBuilder();

            if (request.TagId > 0)
                return new TagBuilder();

            if (request.Untaged)
                return new UntagedBuilder();

            return null;
        }

        private string GetSelectFields()
        {
            var select = @"
              select Article.Id as 'Article.Id', Article.Name as 'Article.Name', Article.Published as 'Article.Published', 
              Article.ViewsCount as 'Article.ViewsCount', Article.LikesCount as 'Article.LikesCount', 
              Feed.Id as 'Feed.Id', Feed.Name as 'Feed.Name', GROUP_CONCAT(Tag.Name) as 'Tag.Name' ";
            return select;
        }

        internal List<Tag> GetTags()
        {
            return Get("GetTags").ToTags();
        }

        internal List<Tag> GetTags(List<string> names)
        {
            if (names.Count == 0) return new List<Tag>();
            names = names.Distinct().ToList();

            string select = "select * from Tag where ";
            var parameters = new Dictionary<string, object>();
            for (int i = 0; i < names.Count; i++)
            {
                var parameterName = "?name" + i;

                select += "lower(Name) = " + parameterName + " or ";
                parameters.Add(parameterName, names[i].Trim().ToLower());
            }

            select = select.Substring(0, select.Length - 3);

            return GetFromSelect(select, parameters).ToTags();
        }

        public List<Tag> GetTagsContaining(string chars, int count)
        {
            if (chars.IsNullOrEmpty()) return new List<Tag>();
            var names = chars.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (names.Count == 0) return new List<Tag>();

            var parameters = new Dictionary<string, object>();

            var select = "select * from Tag where ";
            foreach (var key in names)
            {
                int index = names.IndexOf(key);

                if (index > 0) select += " or ";
                select += " Name like ?name" + index;
                parameters.Add("?name" + index, "%" + key.ToLower() + "%");
            }
            select += " order by length(Name) limit 0," + count;

            return GetFromSelect(select, parameters).ToTags();
        }

        internal List<Tag> GetTags(List<int> tagIds)
        {
            if (tagIds.Count == 0) return new List<Tag>();

            var select = "select * from Tag where ";
            foreach (var id in tagIds.Distinct())
            {
                select += " Id = " + id + " or ";
            }
            select = select.Substring(0, select.Length - 3);

            return GetFromSelect(select).ToTags();
        }

        public List<Tag> GetTagsWithSimilarAndSynonims(List<int> tagIds)
        {
            if (tagIds.Count == 0) return new List<Tag>();

            var select = "select * from Tag where ";
            foreach (var id in tagIds.Distinct())
            {
                select += " Id = " + id + " or SynonimTagId = " + id + " or find_in_set('" + id + "', SimilarTagIds) or ";
            }
            select = select.Substring(0, select.Length - 3);

            return GetFromSelect(select).ToTags();
        }

        internal void UpdateTagArticleCount(int tagId)
        {
            ExecuteStoredProcedure("UpdateTagArticleCount", new Dictionary<string, object>
                {
                    {"tagId", tagId }
                });
        }

        internal void UpdateTag(Tag tag)
        {
            ExecuteStoredProcedure("UpdateTag",
                new Dictionary<string, object>
                {
                    { "id", tag.Id },
                    { "active", tag.Active },
                    { "name", tag.Name },
                    { "description", tag.Description },
                    { "synonimtag", tag.SynonimTagId },
                    { "similartagids", tag.SimilarTagIds.ToCommaSeparetedListOfIds() },
                    { "subscriberscount", tag.SubscribersCount }
                });
        }

        internal void UpdateArticleTag(int articleId, Tag tag)
        {
            ExecuteStoredProcedure("UpdateArticleTag",
                new Dictionary<string, object>
                {
                    { "articleid", articleId },
                    { "tagid", tag.Id },
                    { "approved", tag.Approved },
                    { "approvedby", tag.ApprovedBy.ToCommaSeparetedListOfIds() },
                    { "rejectedby", tag.RejectedBy.ToCommaSeparetedListOfIds() }
                });
        }

        internal Tag GetTag(int id)
        {
            var select = "select * from Tag where Id = " + id;
            return GetFromSelect(select).ToTags().FirstOrDefault();
        }

        internal Tag GetTag(string name)
        {
            if (name.IsNullOrEmpty()) return null;
            var select = "SELECT * from Tag where name = ?name ";

            return GetFromSelect(select, new Dictionary<string, object>
            {
                { "?name", name }
            })
            .ToTags()
            .FirstOrDefault();
        }

        internal List<Vote> GetUserVotes(int userId)
        {
            return GetFromSelect("select * from UserArticleVote where UserId = " + userId)
                .ToArticleVotes();
        }

        internal List<FavoriteArticle> GetFavoriteArticlesForUser(int userId)
        {
            return GetFromSelect("select * from UserFavoriteArticle where UserId = " + userId)
                .ToFavoriteArticles();
        }

        internal void UpdateArticleAsRead(int userId, int articleId)
        {
            ExecuteStoredProcedure("UpdateArticleAsRead", new Dictionary<string, object>
             {
                 { "userid", userId },
                 { "articleid", articleId }
             });
        }

        internal void IncreaseUserReputation(List<int> userIds, int total)
        {
            foreach (var userId in userIds)
            {
                GetFromSelect("update User set Reputation = Reputation + " + total + " where Id = " + userId);
            }
        }

        public int InsertArticleTag(Article article, Tag tag)
        {
            if (article == null || article.Id <= 0 || tag == null) return 0;

            return Get("InsertArticleTag", new Dictionary<string, object>
            {
                { "articleId",  article.Id },
                { "articlePublished",  article.Published },
                { "tagId", tag.Id },
                { "submittedby", tag.SubmittedBy },
                { "approved", tag.Approved },
                { "approvedby", tag.ApprovedBy.ToCommaSeparetedListOfIds() },
            }).ToIdentity();
        }

        internal List<Tag> GetTagsWithArticlesCountGreaterThan(int count)
        {
            return GetFromSelect("select * from Tag where ArticlesCount >= " + count + " order by ArticlesCount desc")
                .ToTags();
        }

        internal List<Tag> GetTagsForArticle(int articleId)
        {
            return GetFromSelect("select * from Tag where Id in (select TagId from ArticleTag where ArticleId = " + articleId + ")")
                .ToTags();
        }

        internal void DeleteArticleTag(int articleId, string tagName)
        {
            var select = "delete from ArticleTag where ArticleId = " + articleId + " and TagId in ";
            select += "(";
            select += " select Id from Tag where Name = '" + tagName + "'";
            select += ")";
            GetFromSelect(select);
        }

        internal List<Tag> GetArticleTags(int articleId)
        {
            var select = @"select at.TagId as Id, at.Approved, at.ApprovedBy, at.RejectedBy, t.Name
                          from ArticleTag at
                          inner join Tag t on t.Id = at.TagId
                          where at.ArticleId = " + articleId;

            return GetFromSelect(select)
                .ToTags();
        }

        internal Tag GetArticleTag(string tagName, int articleId)
        {
            var select = @"select at.TagId as Id, at.Approved, at.ApprovedBy, t.Name
                          from ArticleTag at
                          inner join Tag t on t.Id = at.TagId
                          where at.ArticleId = " + articleId + @"
                          and t.Name = '" + tagName + "'";

            return GetFromSelect(select)
                .ToTags().FirstOrDefault();
        }

        internal List<UserFeed> GetUserFeeds(int userId)
        {
            var select = "select * from UserFeed where FeedId in (select FeedId from Article) and UserId = " + userId;
            return GetFromSelect(select).ToUserFeeds();
        }

        internal List<Feed> GetUserFavoriteFeeds(int userId)
        {
            var select = "select * from Feed where Id in (" +
                "select FeedId from UserFeed where Subscribed = 1 and UserId = " + userId +
                ")";
            return GetFromSelect(select).ToFeeds();
        }

        internal Feed GetFeed(string url)
        {
            var select = "select * from Feed where LOWER(SiteUrl) = ?url";
            var feed = GetFromSelect(select, new Dictionary<string, object>
            {
                { "?url",  url.ToLower() }
            })
            .ToFeeds()
            .FirstOrDefault();

            if (feed != null) return feed;

            return GetFeedsLike(url);
        }

        internal Feed GetFeedByXmlUrl(string rssUrl)
        {
            var select = "select * from Feed where LOWER(Url) = ?url";

            return GetFromSelect(select, new Dictionary<string, object>
            {
                { "?url",  rssUrl.ToLower() }
            })
            .ToFeeds()
            .FirstOrDefault();
        }

        private Feed GetFeedsLike(string siteUrl)
        {
            var select = "select * from Feed where LOWER(SiteUrl) like ?url";
            return GetFromSelect(select, new Dictionary<string, object>
            {
                { "?url", "%" + siteUrl.ToLower() + "%" }
            })
            .ToFeeds()
            .FirstOrDefault();
        }

        internal User GetUser(string remoteId, LoginProvider loginProvider)
        {
            var lp = Enum.GetName(typeof(LoginProvider), loginProvider).ToLower();

            var select = "select * from User where RemoteId = ?remoteid and LoginProvider = '" + lp + "'";
            return GetFromSelect(select, new Dictionary<string, object>
            {
                { "?remoteid", remoteId }
            }).ToUsers().FirstOrDefault();
        }

        internal User GetUser(string userName)
        {
            var select = "select * from User where UserName = ?username and LoginProvider = 'Internal'";
            return GetFromSelect(select, new Dictionary<string, object>
            {
                { "?username", userName }
            }).ToUsers().FirstOrDefault();
        }

        internal List<int> GetTagsThatUserLikes(int userId)
        {
            var select = @"select TagId as Id from ArticleTag where Approved = 1 and ArticleId in 
                           (select ArticleId from UserArticleVote where UserId = " + userId + @" and Votes > 0)
                           group by TagId
                           having count(*) > 10
                           order by count(*) desc limit 0,10";
            return GetFromSelect(select)
                    .ToListOfIds();
        }

        internal List<Feed> GetFeedsUpdatedBefore(bool isPublic, DateTime dateTime, int count)
        {
            var select = "select * from Feed where (Updated is null or Updated <= '" + dateTime.ToMySQLString() + "') and Public = " + (isPublic ? "1" : "0") + " limit 0," + count;
            return GetFromSelect(select).ToFeeds();
        }

        internal void UpdateArticlesAsIndexed(List<int> indexed)
        {
            var select = "update Article set Indexed = '" + DateTime.Now.ToMySQLString() + "' where Id in (" + indexed.ToCommaSeparetedListOfIds() + ")";
            GetFromSelect(select);
        }

        internal List<Feed> GetFeedsThatAreNotParsed()
        {
            var select = "select * from Feed where Updated is null";
            return GetFromSelect(select).ToFeeds();
        }

        internal void UpdateLastSeen(int userId)
        {
            var update = "update User set LastSeen = '" + DateTime.Now.ToMySQLString() + "' where Id = " + userId;
            GetFromSelect(update);
        }

        internal void InsertArticleAsIgnored(int articleId, int userId)
        {
            var insert = "insert into UserArticleIgnored (ArticleId, UserId) values (" + articleId + "," + userId + ")";
            GetFromSelect(insert);
        }

        internal void InsertFeedAsIgnored(int feedId, int userId)
        {
            var insert = "insert into UserFeedIgnored (FeedId, UserId) values (" + feedId + "," + userId + ")";
            GetFromSelect(insert);
        }

        internal List<Feed> GetFeedsNotPublicAndReviewed()
        {
            var select = "select * from Feed where Public = 0 and Reviewed = 0";
            return GetFromSelect(select).ToFeeds();
        }

        internal List<Feed> GetFeedsNotPublic()
        {
            var select = "select * from Feed where Public = 0";
            return GetFromSelect(select).ToFeeds();
        }

        internal int GetNoOfUsers()
        {
            var select = "select count(*) from User";
            return Convert.ToInt32(GetFromSelect(select).Tables[0].Rows[0][0]);
        }

        internal int GetNoOfFeeds()
        {
            var select = "select count(*) from Feed";
            return Convert.ToInt32(GetFromSelect(select).Tables[0].Rows[0][0]);
        }

        internal int GetNoOfArticles()
        {
            var select = "select count(*) from Article";
            return Convert.ToInt32(GetFromSelect(select).Tables[0].Rows[0][0]);
        }

        internal User GetUserByGuid(string guid)
        {
            return GetFromSelect("select * from User where GUID = '" + guid + "'").ToUsers().FirstOrDefault();
        }

        internal void InsertNewsletterSent(DateTime dateTime, int userId, string userEmail)
        {
            var insert = "insert into Newsletter (Date, UserId, UserEmail) values ('" + dateTime.ToMySQLString() + "'," + userId + ",@email);";
            GetFromSelect(insert, new Dictionary<string, object>() {
              { "email", userEmail }
            });
        }

        internal List<User> GetUsersSubscribedToNewsletters()
        {
            var select = "select * from User where Subscribed = 1";
            return GetFromSelect(select).ToUsers();
        }

        internal List<DateTime> GetUserNewsletterDates(int userId)
        {
            var select = "select Date from Newsletter where UserId = " + userId;
            var ds = GetFromSelect(select);
            var dates = new List<DateTime>();
            foreach (DataRow dr in ds.Tables[0].Rows)
                dates.Add(dr.ToDateTimeOrMinValue("Date"));
            return dates;
        }

        internal List<Tuple<int, DateTime>> GetUserNewsletterDates(IEnumerable<int> customerIds)
        {
            var select = "select UserId,Date from Newsletter where UserId in (" + customerIds.ToList().ToCommaSeparetedListOfIds() + ")";
            var ds = GetFromSelect(select);
            var result = new List<Tuple<int, DateTime>>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var date = dr.ToDateTimeOrMinValue("Date");
                var userId = dr.ToIntOrZero("UserId");
                result.Add(Tuple.Create(userId, date));
            }
            return result;
        }

        internal int GetArticleShortUrls(string shortUrl)
        {
            return GetFromSelect("select count(*) from Article where ShortUrl = @shortUrl", new Dictionary<string, object>
            {
                { "shortUrl", shortUrl }
            }).ToIdentity();
        }

        internal Article GetArticle(string shorturl)
        {
            return GetFromSelect("select * from Article where ShortUrl = @shortUrl", new Dictionary<string, object>
            {
                { "shortUrl", shorturl }
            }).ToArticles().FirstOrDefault();
        }

        internal List<Folder> GetUserFolders(int userId)
        {
            return GetFromSelect("select * from UserFolder where UserId = " + userId)
                .ToFolders();
        }

        internal Folder GetUserFolder(int userId, string name)
        {
            return GetFromSelect("select * from UserFolder where UserId = " + userId + " and Name = @name", new Dictionary<string, object>
            {
                { "name", name}
            }).ToFolders().FirstOrDefault();
        }

        internal void DeleteUserFolder(int userId, int folderId)
        {
            var select = "delete from UserFeedFolder where FolderId in (select Id from UserFolder where UserId = " + userId + " and Id = " + folderId + ");" + Environment.NewLine;
            select += "delete from UserFolder where UserId = " + userId + " and Id = " + folderId + ";";

            GetFromSelect(select);
        }

        internal int InsertUserFolder(int userId, Folder folder)
        {
            var select =
            @"insert into UserFolder 
		    (UserId, Name, Description) values 
            (@userId, @name, @description);

		    select @@identity;";

            return GetFromSelect(select,
                new Dictionary<string, object>
                {
                    { "userid", folder.UserId },
                    { "name", folder.Name },
                    { "description", folder.Description }
                }).ToIdentity();
        }

        internal List<Folder> GetUserFolderAvailableForFeed(int userId, int feedId)
        {
            return GetFromSelect("select * from UserFolder where UserId = " + userId + " and Id not in (select FolderId from UserFeedFolder where UserId = " + userId + " and FeedId = " + feedId + ")")
                .ToFolders();
        }

        internal void InsertUserFeedFolder(int userId, int feedId, int folderId)
        {
            GetFromSelect("insert into UserFeedFolder (UserId, FeedId, FolderId) values (" + userId + "," + feedId + "," + folderId + ")");
        }

        internal List<Feed> GetFeedsByUserFolder(int userId, int folderId)
        {
            return GetFromSelect("select * from Feed where Id in (select FeedId from UserFeedFolder where UserId = " + userId + " and FolderId = " + folderId + ")")
                .ToFeeds();
        }

        internal Folder GetUserFolder(int userId, int folderId)
        {
            return GetFromSelect("select * from UserFolder where Id = " + folderId + " and UserId = " + userId)
                .ToFolders().FirstOrDefault();
        }

        internal List<Folder> GetUserFoldersForFeed(int userId, int feedId)
        {
            return GetFromSelect("select * from UserFolder where Id in (select FolderId from UserFeedFolder where UserId = " + userId + " and FeedId = " + feedId + ")")
                .ToFolders();
        }

        internal void DeleteUserFeedFromFolder(int userId, int folderId, int feedId)
        {
            GetFromSelect("delete from UserFeedFolder where UserId = " + userId + " and FolderId = " + folderId + " and FeedId = " + feedId);
        }

        internal void DeleteArticle(int feedId, string url)
        {
            GetFromSelect(@"
                delete from UserArticleIgnored where ArticleId in (select Id from Article where FeedId = @feedid and Url = @url);
                delete from UserReadArticle where ArticleId in (select Id from Article where FeedId = @feedid and Url = @url);
	            delete from ArticleTag where ArticleId in (select Id from Article where FeedId = @feedid and Url = @url);
	            delete from Article where FeedId = @feedid and Url = @url;", new Dictionary<string, object>
                                                     {
                                                         { "feedid", feedId },
                                                         { "url", url }
                                                     });
        }

        internal List<int> GetArticleVotes(int articleId)
        {
            var ds = GetFromSelect("select UserId from UserFavoriteArticle where ArticleId = " + articleId);
            var result = new List<int>();
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                result.Add(dr.ToIntOrZero("UserId"));
            }
            return result;
        }

        internal void UpdateUserPassword(int userId, byte[] salt, byte[] hash)
        {
            GetFromSelect("update User set Salt = @salt, Password = @password where Id = @id", new Dictionary<string, object>
            {
                { "salt", salt },
                { "password", hash },
                { "id", userId },
            });
        }

        internal Article GetArticleWithNotApprovedTags()
        {
            var art = GetFromSelect("select * from Article where Id in (select ArticleId from ArticleTag where Approved = 0) limit 0,1")
                .ToArticles()
                .FirstOrDefault();

            return art;
        }

        internal void InsertOPML(int userId, string fileName)
        {
            GetFromSelect("insert into UserOPML (UserId, FileName) values (@userId, @fileName);", new Dictionary<string, object>
            {
                { "userId", userId },
                { "fileName", fileName }
            });
        }

        internal List<OPML> GetOPMLFilesToParse()
        {
            var select = "select * from UserOPML where Parsed = 0;";
            var ds = GetFromSelect(select);
            return ds.ToOPML();
        }

        internal void UpdateOPMLAsParsed(int id)
        {
            var select = "update UserOPML set Parsed = 1 where Id = " + id;
            GetFromSelect(select);
        }

        internal void UpdateTagArticleCounts()
        {
            GetFromSelect("update Tag t set ArticlesCount = (select count(*) from ArticleTag where TagId = t.Id)");
        }

        internal List<Tag> GetTags(TagsRequest request)
        {
            var select = "select * from Tag ";
            if (request.Popular)
            {
                select += " order by SubscribersCount desc ";
            }
            if (request.Name)
            {
                select += " order by Name ";
            }
            if (request.New)
            {
                select += " order by Date desc ";
            }
            if (!request.SearchQuery.IsNullOrEmpty())
            {
                select += " where Name like ?name ";
            }
            select += " limit " + (request.Page - 1) * request.PageSize + "," + request.PageSize;

            var tags = GetFromSelect(select, new Dictionary<string, object>
                {
                    { "?name", "%" + request.SearchQuery + "%" }
                }).ToTags();
            foreach (var tag in tags.ToList())
            {
                if (tag.SynonimTagId > 0)
                {
                    var oTag = tags.Find(t => t.Id == tag.SynonimTagId);
                    if (oTag != null)
                    {
                        oTag.ArticlesCount += tag.ArticlesCount;
                        tags.Remove(tag);
                    }
                }
            }
            return tags;
        }

        internal Payment GetPayment(string transactionId)
        {
            return GetFromSelect("select * from Payment where TransactionId = ?transaction", new Dictionary<string, object>
            {
                { "?transaction", transactionId }
            }).ToPayments().FirstOrDefault();
        }

        internal List<Payment> GetPayments(int userId)
        {
            return GetFromSelect("select * from Payment where UserId = " + userId).ToPayments();
        }

        internal int InsertPayment(Payment order)
        {
            var insert =
            @"insert into Payment (UserId, TransactionId, OrderType, Amount, Email, Date, FormValues) 
            values (@userId, @transactionId, @orderType, @amount, @email, @date, @formValues);

		    select @@identity;";

            order.Date = DateTime.Now;

            return GetFromSelect(insert,
                new Dictionary<string, object>
                {
                    { "userId", order.UserId },
                    { "transactionId", order.TransactionId },
                    { "orderType", order.OrderType },
                    { "amount", order.Amount },
                    { "email", order.Email },
                    { "date", order.Date.ToMySQLString() },
                    { "formValues", order.FormValues }
                }).ToIdentity();
        }

        internal void AddSearch(string search, User user)
        {
            var insert =
            @"insert into UserSearch (UserId, Search, Date) 
            values (@userId, @search, @date);";

            GetFromSelect(insert,
                new Dictionary<string, object>
                {
                    { "userId", user.Id },
                    { "search", search },
                    { "date", DateTime.Now.ToMySQLString() }
                });
        }

        internal int GetUserSearchesForDate(int userId, DateTime dateTime)
        {
            var select = "select count(*) from UserSearch where UserId = " + userId;
            select += " and Date >= '" + dateTime.Date.ToMySQLString() + "' ";
            select += " and Date <= '" + dateTime.Date.AddDays(1).AddSeconds(-1).ToMySQLString() + "'";
            return GetFromSelect(select).ToIdentity();
        }

        internal void SetFeedsAsUpdated(IEnumerable<int> feedIds)
        {
            var update = "update Feed set Updated = '" + DateTime.Now.ToMySQLString() + "' where Id in (" + string.Join(",", feedIds) + ")";
            GetFromSelect(update);
        }

        internal List<Feed> GetSubmitedFeeds(int userId)
        {
            var select = "select * from Feed where Id in (select FeedId from UserFeed where Submited = 1 and UserId = " + userId + ")";
            return GetFromSelect(select).ToFeeds();
        }

        internal List<UserTaggedArticle> GetTaggedArticlesStatuses(int userId)
        {
            var select = "select Approved, ArticleId, TagId from ArticleTag where SubmittedBy = " + userId;
            return GetFromSelect(select).ToUserTaggedArticles();
        }

        internal List<Article> GetArticlesPublishedAfterForTag(DateTime dateTime, int tagId)
        {
            var select = @"select Id, FeedId, Name, Url, ViewsCount, LikesCount, FavoriteCount, 
                            Published, ShortUrl from Article where 
                            FeedId in (select Id from Feed where Public = 1) and Flagged = 0 and
                            Id in (select ArticleId from ArticleTag where TagId = " + tagId;
            select += @") and
                            Published >= '" + dateTime.ToMySQLString() + "'";
            return GetFromSelect(select).ToArticles();
        }

        internal List<int> GetVisitedArticlesPublishedAfter(int userId, DateTime dateTime)
        {
            var select = "select ArticleId from UserReadArticle ura ";
            select += " inner join Article a on ura.ArticleId = a.Id ";
            select += " where ura.UserId = " + userId + " and ura.ArticleId in (select Id from Article where a.Published >= '" + dateTime.ToMySQLString() + "')";

            var ds = GetFromSelect(select);
            return ds.Tables[0].Rows.OfType<DataRow>().Select(dr => Convert.ToInt32(dr[0])).ToList();
        }

        internal List<Article> GetArticles(int feedId)
        {
            return GetFromSelect("select * from Article where FeedId = " + feedId).ToArticles();
        }

        internal List<Article> GetArticles(List<int> ids)
        {
            return GetFromSelect("select * from Article where Id in (" + ids.ToCommaSeparetedListOfIds() + ")").ToArticles();
        }

        internal bool SaveMetadata(MetaEntity entity, List<KeyValuePair<string, string>> metadata)
        {
            var localItems = GetAllMetadata(entity);

            //Create & Update
            foreach (var item in metadata)
            {
                var localItem = localItems.Find(md => md.Key == item.Key);

                var isLocalItemNull = localItem.Equals(new KeyValuePair<string, string>());
                if (isLocalItemNull)
                {
                    var insert = "insert into metadata (entity,entityid,`key`,`value`) values ";
                    insert += "('" + entity.EntityName + "'," + entity.EntityId + ",'" + item.Key + "','" + item.Value + "')";
                    GetFromSelect(insert);
                }
                else if (localItem.Value != item.Value.ToString())
                {
                    var update = "update metadata set updated = '" + DateTime.Now.ToMySQLString();
                    update += "', value ='" + item.Value + "' where entity = '" + entity.EntityName + "'";
                    update += " and entityid = " + entity.EntityId;
                    update += " and `key` = '" + item.Key + "'";
                    GetFromSelect(update);
                }
                else
                {
                    localItems.Remove(localItem);
                }
            }

            //Delete
            foreach (var localItem in localItems)
            {
                var delete = "delete from metadata where entity = '" + entity.EntityName + "' and entityid = " + entity.EntityId;
                delete += " and `key` = '" + localItem.Key + "' and value = '" + localItem.Value + "'";
                GetFromSelect(delete);
            }

            return true;
        }

        internal List<KeyValuePair<string, string>> GetAllMetadata(MetaEntity entity)
        {
            var select = "select * from metadata where entity = '" + entity.EntityName + "' and entityid = " + entity.EntityId;
            return GetFromSelect(select).ToKeyValuePairs();
        }
    }
}
