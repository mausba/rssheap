using Core.Caching;
using Core.Enums;
using Core.Models;
using Core.Services;
using Core.Utilities;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Caching
{
    public static class Redis
    {
        private static FeedService FeedService = new FeedService();
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost,abortConnect=false"));
        private static ConnectionMultiplexer Connection { get { return lazyConnection.Value; } }

        static Redis()
        {
            FeedService = new FeedService();
        }

        /// <summary>
        /// Gets the articles that are cached in Redis
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="type">week or month</param>
        /// <returns></returns>
        public static List<Article> GetArticles(int userId, string type)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();
                    var key = type == "week" ? GetWeekKey(userId) : GetMonthKey(userId);

                    var weekArticles = db.SortedSetRangeByRank(key, order: Order.Descending);
                    var tempArticles = new List<Article>();
                    foreach (RedisValue a in weekArticles)
                    {
                        var hsArticle = db.HashGetAll(GetArticleKey((int)a));
                        var article = ToArticle(hsArticle);
                        if (article != null)
                            tempArticles.Add(article);
                    }
                    return tempArticles;
                }, exceptionMessage: "GetArticles:userid:" + userId + " type:" + type);
        }

        public static bool IgnoreArticle(int userId, int articleId)
        {
            throw new NotImplementedException();
        }

        public static bool AddArticle(Article article)
        {
            return Retry3TimesForTimeout(() =>
                {
                    if (article == null || !article.Feed.Public || article.Published < DateTime.Now.Date.AddMonths(-1)) return false;
                    var key = GetArticleKey(article.Id);

                    var db = GetDatabase();
                    db.HashSet(key, ToHashEntries(article));
                    var expiryDate = article.Published.Date.AddMonths(1).AddDays(1) - DateTime.Now.Date;
                    db.KeyExpire(key, expiryDate);

                    AddArticleToAssignedUsers(article);

                    return true;
                }, exceptionMessage: "AddArticle:article:" + article.ToXml());
        }

        private static bool AddArticleToAssignedUsers(Article article)
        {
            return Retry3TimesForTimeout(() =>
                {
                    //add it for all the users that are subscribed to this article
                    if (article == null || !article.Feed.Public || article.Published < DateTime.Now.Date.AddMonths(-1)) return false;

                    var db = GetDatabase();
                    var users = GetUsers();

                    var tasks = new List<Task>();
                    foreach (var user in users)
                    {
                        var isInFavoriteTags = article.Tags.Any(t => user.FavoriteTagIds.Contains(t.Id));
                        var isInIngoredTags = article.Tags.Any(t => user.IgnoredTagIds.Contains(t.Id));

                        if (isInFavoriteTags && !isInIngoredTags)
                        {
                            var whereToPutIt = article.Published >= DateTime.Now.Date.AddDays(-7)
                                ? GetWeekKey(user.Id) : GetMonthKey(user.Id);

                            tasks.Add(db.SortedSetAddAsync(whereToPutIt, article.Id, article.LikesCount));
                        }
                    }
                    db.WaitAll(tasks.ToArray());

                    //add article for users that have no tags defined
                    var noTagKey = article.Published >= DateTime.Now.Date.AddDays(-7) ? GetWeekNoTagsKey() : GetMonthNoTagsKey();
                    db.SortedSetAdd(noTagKey, article.Id, article.LikesCount);

                    return true;
                }, exceptionMessage: "AddArticleToAssignedUsers:article:" + article.ToXml());
        }

        public static bool AddArticleTag(Article article, int tagId)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var users = GetUsers();
                    foreach (var user in users)
                    {
                        var isUserFavoriteTag = user.FavoriteTagIds.Any(t => t == tagId);
                        var isInUserIgnoreList = user.IgnoredTagIds.Any(t => article.Tags.Select(tag => tag.Id).Contains(t));

                        if (isInUserIgnoreList && !isInUserIgnoreList)
                        {
                            AddArticleToAssignedUsers(article);
                        }
                    }
                    return true;
                }, exceptionMessage: "AddArticleTag:article:" + article.ToXml() + " tag:" + tagId);
        }

        public static void VoteUpArticle(Article article)
        {
            if (article.Published < DateTime.Now.Date.AddMonths(-1)) return;

            Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();

                    var tasks = new List<Task>
                    {
                        db.HashSetAsync(GetArticleKey(article.Id), "LikesCount", article.LikesCount + 1)
                    };
                    var users = GetUsers();
                    foreach (var user in users)
                    {
                        void IncrementScore(string key)
                        {
                            if (db.SortedSetScore(key, article.Id).HasValue)
                            {
                                tasks.Add(db.SortedSetIncrementAsync(key, article.Id, 1));
                            }
                        }

                        IncrementScore(GetWeekKey(user.Id));
                        IncrementScore(GetMonthKey(user.Id));
                    }

                    db.WaitAll(tasks.ToArray());
                    return false;
                }, exceptionMessage: "VoteUpArticle:article:" + article.ToXml());
        }

        public static void VoteDownArticle(Article article)
        {
            Retry3TimesForTimeout(() =>
            {
                var db = GetDatabase();

                var tasks = new List<Task>
                {
                    db.HashSetAsync(GetArticleKey(article.Id), "LikesCount", article.LikesCount - 1)
                };

                var users = GetUsers();
                foreach (var user in users)
                {
                    void DecrementScore(string key)
                    {
                        if (db.SortedSetScore(key, article.Id).HasValue)
                        {
                            tasks.Add(db.SortedSetDecrementAsync(key, article.Id, 1));
                        }
                    }

                    DecrementScore(GetWeekKey(user.Id));
                    DecrementScore(GetMonthKey(user.Id));
                }

                db.WaitAll(tasks.ToArray());
                return false;
            }, exceptionMessage: "VoteDownArticle:article:" + article.ToXml());
        }

        public static void RemoveArticleTag(Article article, int tagId)
        {
            if (article.Published < DateTime.Now.Date.AddMonths(-1)) return;

            Retry3TimesForTimeout(() =>
                {
                    var users = GetUsers();
                    var db = GetDatabase();
                    var tasks = new List<Task>();
                    foreach (var user in users.Where(u => u.FavoriteTagIds.Contains(tagId)))
                    {
                        tasks.Add(db.SortedSetRemoveAsync(GetWeekKey(user.Id), article.Id));
                        tasks.Add(db.SortedSetRemoveAsync(GetMonthKey(user.Id), article.Id));
                    }

                    var articleHasNoMoreTags = article.TagsLoaded && article.Tags.Select(t => t.Id).ToList().FindAll(t => t != tagId).Count == 0;
                    if (articleHasNoMoreTags)
                    {
                        var noTagKey = article.Published >= DateTime.Now.Date.AddDays(-7) ? GetWeekNoTagsKey() : GetMonthNoTagsKey();
                        tasks.Add(db.SortedSetAddAsync(noTagKey, article.Id, article.LikesCount));
                    }

                    db.WaitAll(tasks.ToArray());
                    return true;
                }, exceptionMessage: "RemoveArticleTag:article:" + article.ToXml() + " tag:" + tagId);
        }

        public static bool AddUser(User user)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();
                    db.HashSet(GetUserKey(user.Id), ToHashEntries(user));
                    db.SortedSetAdd(GetUsersKey(), user.Id, user.LastSeen.ToTimestamp());
                    return true;
                }, exceptionMessage: "AddUser:user:" + user.ToXml());
        }

        public static User GetUser(int id)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();
                    var userHash = db.HashGetAll(GetUserKey(id));
                    if (userHash == null)
                    {
                        Mail.SendMeAnEmail("No user found on redis with that id", id.ToString());
                        return null;
                    }
                    return ToUser(userHash);
                }, exceptionMessage: "GetUser:id:" + id);
        }

        public static List<User> GetUsers()
        {
            return Retry3TimesForTimeout(() =>
                {
                    return CacheClient.InMemoryCache.GetOrAdd<List<User>>(GetUsersKey(), CachePeriod.ForHours(1), () =>
                        {
                            var db = GetDatabase();
                            var usersSet = db.SortedSetRangeByScore(GetUsersKey(), order: Order.Descending);
                            var users = new List<User>();
                            foreach (var user in usersSet)
                            {
                                users.Add(GetUser((int)user));
                            }
                            return users;
                        });
                }, exceptionMessage: "GetUsers");
        }

        public static bool HideVisitedArticles(int userId)
        {
            Retry3TimesForTimeout(() =>
                {
                    var visitedArticleIds = FeedService.GetArticlesVisitedAndPublishedAfter(userId, DateTime.Now.AddMonths(-1));
                    var tasks = new List<Task>();
                    var db = GetDatabase();
                    foreach (var articleId in visitedArticleIds)
                    {
                        tasks.Add(db.SortedSetRemoveAsync(GetWeekKey(userId), (RedisValue)articleId));
                        tasks.Add(db.SortedSetRemoveAsync(GetMonthKey(userId), (RedisValue)articleId));
                    }
                    db.WaitAll(tasks.ToArray());
                    return true;
                }, exceptionMessage: "HideVisitedArticles:userId:" + userId);
            return true;
        }

        public static bool ShowVisitedArticles(int userId)
        {
            Retry3TimesForTimeout(() =>
            {
                var visitedArticleIds = FeedService.GetArticlesVisitedAndPublishedAfter(userId, DateTime.Now.AddMonths(-1));
                var visitedArticles = FeedService.GetArticles(visitedArticleIds);
                var tasks = new List<Task>();
                var db = GetDatabase();
                foreach (var article in visitedArticles)
                {
                    if (article.Published >= DateTime.Now.Date.AddDays(-7))
                        tasks.Add(db.SortedSetRemoveAsync(GetWeekKey(userId), article.Id));
                    else
                        tasks.Add(db.SortedSetRemoveAsync(GetMonthKey(userId), article.Id));
                }
                db.WaitAll(tasks.ToArray());
                return true;
            }, exceptionMessage: "ShowVisitedArticles:userId:" + userId);
            return true;
        }

        public static bool AddUserTag(User user, int tagId, List<int> ignoredTagIds = null)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var tagArticles = CacheClient.InMemoryCache.GetOrAdd<List<Article>>("articles" + tagId, CachePeriod.ForHours(1),
                        () => FeedService.GetArticlesPublishedAfterForTag(DateTime.Now.Date.AddMonths(-1), tagId));
                    var db = GetDatabase();

                    if (ignoredTagIds != null && ignoredTagIds.Count > 0)
                    {
                        foreach (var ignoredTagId in ignoredTagIds)
                        {
                            var tagIgnoredArticles = CacheClient.InMemoryCache.GetOrAdd<List<Article>>("ingored-articles" + string.Join(",", ignoredTagIds), CachePeriod.ForHours(1),
                            () => FeedService.GetArticlesPublishedAfterForTag(DateTime.Now.Date.AddMonths(-1), ignoredTagId));
                            tagArticles = tagArticles.FindAll(a => !tagIgnoredArticles.Select(ta => ta.Id).Contains(a.Id));
                        }
                    }

                    if (user.HideVisitedArticles)
                    {
                        var visitedArticleIds = FeedService.GetArticlesVisitedAndPublishedAfter(user.Id, DateTime.Now.Date.AddMonths(-1));
                        tagArticles = tagArticles.FindAll(a => !visitedArticleIds.Contains(a.Id));
                    }

                    db.SortedSetAdd(GetWeekKey(user.Id), tagArticles.FindAll(a => a.Published >= DateTime.Now.Date.AddDays(-7))
                                                                    .Select(a => new SortedSetEntry(a.Id, a.LikesCount))
                                                                    .ToArray());
                    db.SortedSetAdd(GetMonthKey(user.Id), tagArticles.FindAll(a => a.Published < DateTime.Now.Date.AddDays(-7) &&
                                                                                   a.Published >= DateTime.Now.Date.AddMonths(-1))
                                                                     .Select(a => new SortedSetEntry(a.Id, a.LikesCount))
                                                                     .ToArray());
                    //AddUser(user);
                    return true;
                }, exceptionMessage: "AddUserTag:user:" + user.ToXml() + " tag:" + tagId + ":ignoredtagIds:" + ignoredTagIds.ToXml());
        }

        public static bool RemoveUserTag(User user, int tagId)
        {
            return Retry3TimesForTimeout(() =>
                {
                    var articles = FeedService.GetArticlesPublishedAfterForTag(DateTime.Now.Date.AddMonths(-1), tagId);
                    var db = GetDatabase();
                    db.SortedSetRemove(GetWeekKey(user.Id), articles.Select(a => (RedisValue)a.Id).ToArray());
                    db.SortedSetRemove(GetMonthKey(user.Id), articles.Select(a => (RedisValue)a.Id).ToArray());
                    user.Tags = user.Tags.FindAll(t => t.Id != tagId);
                    //AddUser(user);
                    return true;
                }, exceptionMessage: "RemoveUserTag:user:" + user.ToXml() + " tag:" + tagId);
        }

        public static void DeleteUser(int user)
        {
            Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();
                    db.SortedSetRemove(GetUsersKey(), user);
                    db.KeyDelete(GetWeekKey(user));
                    db.KeyDelete(GetMonthKey(user));
                    return true;
                }, exceptionMessage: "DeleteUser:user:" + user);
        }

        private static IDatabase GetDatabase()
        {
            return Retry3TimesForTimeout(() =>
                {
                    return Connection.GetDatabase();
                }, exceptionMessage: "GetDatabase");
        }

        private static HashEntry[] ToHashEntries(Article article)
        {
            var hashFields = new List<HashEntry>
            {
                new HashEntry("Id", article.Id),
                new HashEntry("Created", article.Created.ToTimestamp()),
                new HashEntry("FeedId", article.FeedId),
                new HashEntry("Feed.Name", article.Feed.Name ?? string.Empty),
                new HashEntry("Name", article.Name ?? string.Empty),
                new HashEntry("Url", article.Url),
                new HashEntry("ViewsCount", article.ViewsCount),
                new HashEntry("LikesCount", article.LikesCount),
                new HashEntry("Published", article.Published.ToTimestamp()),
                new HashEntry("ShortUrl", article.ShortUrl),
                new HashEntry("Tags", string.Join(",", article.Tags.Select(a => a.Name)))
            };
            return hashFields.ToArray();
        }

        private static HashEntry[] ToHashEntries(User user)
        {
            var hashFields = new List<HashEntry>
            {
                new HashEntry("Id", user.Id),
                new HashEntry("RemoteId", user.RemoteId ?? string.Empty),
                new HashEntry("LoginProvider", Enum.GetName(typeof(LoginProvider), user.LoginProvider)),
                new HashEntry("UserName", user.UserName),
                new HashEntry("Email", user.Email),
                new HashEntry("FirstName", user.FirstName ?? string.Empty),
                new HashEntry("LastName", user.LastName ?? string.Empty),
                new HashEntry("TagIds", string.Join(",", user.FavoriteTagIds)),
                new HashEntry("IgnoredTagIds", string.Join(",", user.IgnoredTagIds)),
                new HashEntry("Guid", user.GUID),
                new HashEntry("Subscribed", user.Subscribed),
                new HashEntry("LastSeen", user.LastSeen.ToTimestamp()),
                new HashEntry("HideVisitedArticles", user.HideVisitedArticles)
            };
            return hashFields.ToArray();
        }

        public static void UpdateArticleName(Article article)
        {
            Retry3TimesForTimeout(() =>
                {
                    var db = GetDatabase();
                    var hashFields = new List<HashEntry>
                    {
                        new HashEntry("Name", article.Name)
                    };
                    db.HashSet(GetArticleKey(article.Id), hashFields.ToArray());
                    return true;
                }, exceptionMessage: "UpdateArticleName:article:" + article.ToXml());
        }

        private static User ToUser(HashEntry[] entries)
        {
            if (entries == null || entries.Count() == 0) return null;
            var u = new User
            {
                Id = (int)GetValueByKey(entries, "Id"),
                RemoteId = GetValueByKey(entries, "RemoteId"),
                LoginProvider = (LoginProvider)Enum.Parse(typeof(LoginProvider), GetValueByKey(entries, "LoginProvider")),
                UserName = GetValueByKey(entries, "UserName").ToString(),
                Email = GetValueByKey(entries, "Email").ToString(),
                FirstName = GetValueByKey(entries, "FirstName"),
                LastName = GetValueByKey(entries, "LastName").ToString(),
                FavoriteTagIds = GetValueByKey(entries, "TagIds").ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).ToList(),
                IgnoredTagIds = GetValueByKey(entries, "IgnoredTagIds").ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).ToList(),
                GUID = GetValueByKey(entries, "Guid").ToString(),
                Subscribed = (bool)GetValueByKey(entries, "Subscribed"),
                LastSeen = ((long)GetValueByKey(entries, "LastSeen")).ToDateTime(),
                HideVisitedArticles = (bool)GetValueByKey(entries, "HideVisitedArticles")
            };
            return u;
        }

        private static Article ToArticle(HashEntry[] entries)
        {
            if (entries == null || entries.Count() == 0) return null;
            var a = new Article
            {
                Id = (int)GetValueByKey(entries, "Id"),
                Created = ((long)GetValueByKey(entries, "Created")).ToDateTime(),
                Name = GetValueByKey(entries, "Name").ToString(),
                FeedId = (int)GetValueByKey(entries, "FeedId"),
                LikesCount = (int)GetValueByKey(entries, "LikesCount"),
                Published = ((long)GetValueByKey(entries, "Published")).ToDateTime(),
                ShortUrl = GetValueByKey(entries, "ShortUrl").ToString(),
                Tags = GetValueByKey(entries, "Tags")
                            .ToString()
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => new Tag { Name = s })
                            .ToList(),
                Url = GetValueByKey(entries, "Url").ToString(),
                ViewsCount = (int)GetValueByKey(entries, "ViewsCount")
            };
            a.Feed = new Feed
            {
                Id = a.FeedId,
                Name = GetValueByKey(entries, "Feed.Name").ToString()
            };
            return a;
        }

        private static string GetWeekKey(int userId) { return "week:articles:user:" + userId; }
        private static string GetMonthKey(int userId) { return "month:articles:user:" + userId; }
        private static string GetWeekNoTagsKey() { return "week:articles:notags"; }
        private static string GetMonthNoTagsKey() { return "month:articles:notags"; }
        private static string GetUsersKey() { return "users"; }
        private static string GetUserKey(int userId) { return "user:" + userId; }
        private static string GetArticlesKey() { return "articles"; }
        private static string GetArticleKey(int articleId) { return "article:" + articleId; }

        private static RedisValue GetValueByKey(HashEntry[] entries, string key)
        {
            return entries.First(e => e.Name == key).Value;
        }

        private static T Retry3TimesForTimeout<T>(Func<T> func, int retryCount = 0, string exceptionMessage = "")
        {
            try
            {
                return func();
            }
            catch (TimeoutException ex)
            {
                if (retryCount <= 3)
                {
                    Thread.Sleep(500);
                    retryCount++;
                    return Retry3TimesForTimeout(func, retryCount, exceptionMessage);
                }

                Mail.SendMeAnEmail("Retried function for 3 times", exceptionMessage + " " + ex.ToString());
            }
            catch (RedisConnectionException ex)
            {
                if (retryCount <= 3)
                {
                    Thread.Sleep(500);
                    retryCount++;
                    return Retry3TimesForTimeout(func, retryCount);
                }

                Mail.SendMeAnEmail("Retried function for 3 times", func.Method.Name.ToString() + " " + ex.ToString());
            }
            catch (Exception ex)
            {
                if (retryCount <= 3)
                {
                    Thread.Sleep(500);
                    retryCount++;
                    return Retry3TimesForTimeout(func, retryCount);
                }
                Mail.SendMeAnEmail("Exception in redis ", func.Method.Name.ToString() + " " + ex.ToString());
            }
            return default(T);
        }
    }
}
