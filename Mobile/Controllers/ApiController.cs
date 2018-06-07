using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Enums;
using Core.Extensions;
using Core.Models;
using Core.Models.Requests;
using Core.Services;
using Core.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Caching;
using Core;
using PWDTK_DOTNET451;

namespace Web.Controllers
{
    //[RequiresSSL]
    public class ApiController : Controller
    {
        private UserService UserService = new UserService();
        private FeedService FeedService = new FeedService();
        private string SecretGuid = "f123c52cd1e2442290c57f353250b232";

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.ContentType = "application/json";
            base.OnActionExecuting(filterContext);
        }

        public ActionResult AddTag()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var tag = json.Value<string>("tag");
            var articleId = json.Value<int>("id");

            FeedService.SuggestTag(articleId, tag, user);
            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult RemoveTag()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var tag = json.Value<string>("tag");
            var articleId = json.Value<int>("id");

            FeedService.DeleteArticleTag(articleId, tag);
            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult Tags()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var tag = json.Value<string>("tag");

            var tags = FeedService.GetTagsContaining(tag, 3).Select(t => t.Name);

            return Json(new
            {
                tags = tags
            });
        }

        public ActionResult Flag()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var id = json.Value<int>("id");

            var article = FeedService.GetArticle(id);
            if (article == null) return Json(false);

            if (article.FlaggedBy.Contains(user.Id)) return Json(false);
            article.FlaggedBy.Add(user.Id);

            article.Flagged = article.FlaggedBy.Count >= 3 || user.IsAdmin; ;

            FeedService.UpdateArticle(article);

            if (article.Flagged && article.FlaggedBy.Count > 0)
            {
                IISTaskManager.Run(() =>
                {
                    var userToAddRepId = article.FlaggedBy.First();
                    var userToAddRep = UserService.GetUser(userToAddRepId);
                    if (userToAddRep != null)
                    {
                        userToAddRep.Reputation += 2;
                        UserService.UpdateUser(user);
                    }
                });
            }

            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult IncreaseArticleViewCount()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var id = json.Value<int>("id");

            IISTaskManager.Run(() =>
            {
                FeedService.UpdateArticleAsRead(user.Id, id);
            });

            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult ToggleVisited()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            user.HideVisitedArticles = !user.HideVisitedArticles;
            UserService.UpdateUser(user);
            return RefreshUserInfoObject(json);
        }

        public ActionResult AddRemoveTag()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var tagId = json["id"].Value<int>();

            var tag = FeedService.GetTag(tagId);

            if (tag != null)
            {
                if (user.Tags.Any(t => t.Id == tag.Id))
                {
                    user.Tags = user.Tags.Where(t => t.Id != tag.Id).ToList();
                    user.FavoriteTagIds = user.FavoriteTagIds.Where(t => t != tag.Id).ToList();

                    user.IgnoredTagIds.Add(tag.Id);

                    ActionExtensions.TryAction(() =>
                    {
                        IISTaskManager.Run(() =>
                        {
                            tag.SubscribersCount--;
                            FeedService.UpdateTag(tag);
                        });
                    }, "Error in increasing tag subscribers count");
                }
                else
                {
                    user.Tags.Add(tag);
                    user.FavoriteTagIds.Add(tag.Id);

                    user.IgnoredTagIds = user.IgnoredTagIds.Where(t => t != tag.Id).ToList();

                    ActionExtensions.TryAction(() =>
                    {
                        IISTaskManager.Run(() =>
                        {
                            tag.SubscribersCount++;
                            FeedService.UpdateTag(tag);
                        });
                    }, "Error in increasing tag subscribers count");
                }

                UserService.UpdateUser(user);
            }

            return RefreshUserInfoObject(json);
        }

        public ActionResult AddToFavorites()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var articleId = json["id"].Value<int>();

            FeedService.InsertFavoriteArticle(user, articleId);

            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult RemoveFromFavorites()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var articleId = json["id"].Value<int>();

            FeedService.DeleteFavoriteArticle(user, articleId);

            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult VoteDown()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var articleId = json["id"].Value<int>();

            FeedService.DownVote(user, articleId);
            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult VoteUp()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            var articleId = json["id"].Value<int>();

            FeedService.UpVote(user, articleId);
            return Json(new
            {
                ok = "true"
            });
        }

        public ActionResult GetTags()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            int page = json["page"].Value<int>() + 1;

            var request = new TagsRequest();
            request._PageSize = 20;
            request._Page = page;
            request.Popular = true;

            var tags = FeedService.GetTags(request);

            return Json(new
            {
                tags = tags
            });
        }

        public ActionResult GetArticles()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            int page = json["page"].Value<int>() + 1;

            var request = new ArticlesRequest
            {
                _PageSize = 30,
                _Page = page,
                User = user,
                IsFromMobile = true
            };
            var view = json["view"].Value<string>();
            switch (view)
            {
                case "week": request.Week = true; break;
                case "month": request.Month = true; break;
                case "votes": request.Votes = true; break;
                case "untagged": request.Untaged = true; break;
                case "favorites": request.Favorites = true; break;
                case "myfeeds": request.MyFeeds = true; break;
            }

            if (view.StartsWith("feed"))
                request.FeedId = int.Parse(view.Replace("feed", string.Empty));

            if (view.StartsWith("folder"))
                request.FolderId = int.Parse(view.Replace("folder", string.Empty));

            if (view.StartsWith("tag"))
                request.TagId = int.Parse(view.Replace("tag", string.Empty));

            var articles = FeedService.GetArticles(request);

            return Json(new
            {
                articles = articles
            });
        }

        public ActionResult GetArticle()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var user = GetUserCached(json);
            int index = json["index"].Value<int>();
            string platform = string.Empty;
            if (json["platform"] != null)
            {
                platform = json["platform"].Value<string>();
            }

            var request = new ArticlesRequest
            {
                _PageSize = 2,
                _Page = index,
                User = user,
                IsSingleArticleRequest = true,
                IsFromMobile = true,
                AppVersion = 2
            };
            var view = json["view"].Value<string>();
            switch (view)
            {
                case "week": request.Week = true; break;
                case "month": request.Month = true; break;
                case "votes": request.Votes = true; break;
                case "untagged": request.Untaged = true; break;
                case "favorites": request.Favorites = true; break;
                case "myfeeds": request.MyFeeds = true; break;
            }

            if (view.StartsWith("feed"))
                request.FeedId = int.Parse(view.Replace("feed", string.Empty));

            if (view.StartsWith("folder"))
                request.FolderId = int.Parse(view.Replace("folder", string.Empty));

            if (view.StartsWith("tag"))
                request.TagId = int.Parse(view.Replace("tag", string.Empty));

            var articles = FeedService.GetArticles(request);

            return Json(new
            {
                articles
            });
        }

        public ActionResult RefreshUserInfo()
        {
            var json = GetJson(HttpContext.Request);
            return RefreshUserInfoObject(json);
        }

        private ActionResult RefreshUserInfoObject(JObject json)
        {
            ValidateJson(json);
            var user = GetUserCached(json, clearCache: true);
            return Json(new
            {
                feeds = UserService.GetUserFavoriteFeeds(user.Id)
                                    .Select(f => new
                                    {
                                        Id = f.Id,
                                        Name = f.Name,
                                        Favicon = f.Favicon.Replace("default-favicon", "default-favicon-white")
                                    }),
                folders = user.Folders
                              .Select(f => new
                              {
                                  Id = f.Id,
                                  Name = f.Name,
                                  Feeds = FeedService.GetFeedsByUserFolder(user.Id, f.Id)
                              }),
                tags = user.FavoriteTagIds,
                tagsobjects = FeedService.GetTags(user.FavoriteTagIds),
                hidevisited = user.HideVisitedArticles
            });
        }

        public ActionResult GetVersion()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);
            var device = json["device"].Value<string>();

            return Json(new
            {
                version = System.Configuration.ConfigurationManager.AppSettings["mobileversion"]
            });
        }

        [HttpPost]
        [System.Web.Http.HttpPost]
        public ActionResult GetUser()
        {
            var json = GetJson(HttpContext.Request);
            ValidateJson(json);

            User user = null;
            LoginProvider lp = LoginProvider.Internal;
            switch (json["provider"].Value<string>())
            {
                case "google":
                    user = UserService.GetUser(json["id"].Value<string>(), LoginProvider.Google);
                    lp = LoginProvider.Google;
                    break;
                case "twitter":
                    user = UserService.GetUser(json["id"].Value<string>(), LoginProvider.Twitter);
                    lp = LoginProvider.Twitter;
                    break;
                case "facebook":
                    user = UserService.GetUser(json["id"].Value<string>(), LoginProvider.Facebook);
                    lp = LoginProvider.Facebook;
                    break;
                case "internal":
                    string userName = json["username"].Value<string>();
                    string password = json["password"].Value<string>();

                    user = UserService.GetUser(userName);
                    if (user != null)
                    {
                        if (!PWDTK.ComparePasswordToHash(user.Salt, password, user.Password, Configuration.GetHashIterations()))
                        {
                            user = null;
                        }
                    }
                    lp = LoginProvider.Internal;
                    break;
            }


            if (user == null && lp != LoginProvider.Internal)  //create the user if doesn't exist
            {
                user = new User
                {
                    RemoteId = json["id"].Value<string>(),
                    LoginProvider = lp
                };
                switch (lp)
                {
                    case LoginProvider.Twitter:
                        user.UserName = json["screenName"].Value<string>();
                        break;
                    case LoginProvider.Facebook:
                        user.FirstName = json["firstname"].Value<string>();
                        user.LastName = json["lastname"].Value<string>();
                        user.UserName = json["name"].Value<string>();
                        user.Email = json["email"].Value<string>();
                        break;
                    case LoginProvider.Google:
                        user.UserName = json["email"].Value<string>();
                        user.Email = json["email"].Value<string>();
                        break;
                }

                int newId = UserService.InsertUser(user, () => Redis.AddUser(user));
                user = UserService.GetUser(newId);
            }

            return Json(user != null ? new
            {
                id = user.Id,
                guid = user.GUID
            } : null);
        }

        private User GetUserCached(JObject json, bool clearCache = false)
        {
            var userGuid = json["USERGUID"].Value<string>();
            var cacheKey = "user" + userGuid;
            if (clearCache)
                CacheClient.Default.Remove(cacheKey);

            return CacheClient.Default.GetOrAdd<User>(cacheKey, CachePeriod.ForMinutes(5),
                () =>
                {
                    var user = UserService.GetUserByGuid(userGuid);
                    if (user == null) return null;

                    user.Tags = FeedService.GetTags(user.FavoriteTagIds);
                    user.FavoriteTagIds = user.Tags.Select(t => t.Id).ToList();

                    if (user.FavoriteTagIds.Count > 0)
                    {
                        var likedTagIds = FeedService.GetTagsThatUserLikes(user.Id);
                        user.FavoriteTagIds.AddRange(likedTagIds);
                        user.FavoriteTagIds = user.FavoriteTagIds.Distinct().Where(t => !user.IgnoredTagIds.Contains(t)).ToList();
                    }

                    user.IgnoredTags = FeedService.GetTags(user.IgnoredTagIds);
                    user.MyFeeds = UserService.GetUserFeeds(user.Id);
                    user.Folders = UserService.GetUserFolders(user.Id);
                    return user;
                });
        }

        private JObject GetJson(HttpRequestBase request)
        {
            var jsonStr = string.Empty;
            using (var sr = new StreamReader(Request.InputStream))
            {
                jsonStr = sr.ReadToEnd();
            }
            if (jsonStr.IsNullOrEmpty()) throw new Exception("Json string in request stream is empty");
            jsonStr = jsonStr.Replace("(null)=", string.Empty);
            return JsonConvert.DeserializeObject<JObject>(jsonStr);
        }

        private void ValidateJson(JObject json)
        {
            if (json == null) throw new HttpException(403, "You are not allowed to access the resource", new Exception("json string is empty"));
            if (json["GUID"] == null) throw new HttpException(403, "You are not allowed to access the resource", new Exception(json.ToString()));
            var guid = json["GUID"].Value<string>();
            if (guid.IsNullOrEmpty() || guid != SecretGuid) throw new HttpException(403, "You are not allowed to access the resource", new Exception(json.ToString()));
        }
    }
}