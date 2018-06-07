using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Services;
using System.IO;
using System.Web.Script.Serialization;
using System.Threading;
using System.Xml;
using Core.Models;
using System.Net;
using System.Net.Http;
using Core.Parsers;
using MvcWeb.ViewModels;
using HtmlAgilityPack;
using System.ServiceModel.Syndication;
using Core.Models.Requests;
using System.Text;
using Web.Code.ActionFilters;
using Core.Utilities;
using MeasurementProtocolClient;
using Core.Extensions;
using Web.Code.ActionResults;
using Core.Caching;
using System.Collections.Specialized;

namespace MvcWeb.Controllers
{
    public class ArticleController : _BaseController
    {
        [NonSSL]
        public ActionResult Article()
        {
            var reqQuery = Request["p"];
            if (reqQuery.IsNullOrEmpty()) return NotFoundResult();

            var user = CurrentUser;
            var query = HttpUtility.ParseQueryString(reqQuery.FromBase64());
            var guid = query["guid"];
           
            if(user == null)
            {
                if (!guid.IsNullOrEmpty())
                {
                    user = UserService.GetUserByGuid(guid);
                    if (user == null)
                    {
                        System.Web.Security.FormsAuthentication.RedirectToLoginPage();
                        return null;
                    }
                }
                else
                {
                    System.Web.Security.FormsAuthentication.RedirectToLoginPage();
                    return null;
                }
            }
            

            int articleId = int.TryParse(query["articleid"], out articleId) ? articleId : 0;
            if (articleId == 0) return NotFoundResult();

            if (query["next"].ToBool())
            {
                return Next(query);
            }

            var vm = new ArticleVM
            {
                User = user,
                Query = query,
                Article = FeedService.GetArticleWithAssObjects(articleId, user)
            };

            if (vm.Article == null) return NotFoundResult();

            if (Request.Browser.IsMobileDevice)
                return Redirect("http://www.rssheap.com/a/" + vm.Article.ShortUrl);

            return View(vm);
        }

        private ActionResult NotFoundResult()
        {
            return View("../Home/404");
        }

        [Authorize]
        [NonSSL]
        public ActionResult Next(NameValueCollection q)
        {
            var a = q["articleid"].ToInt();
            var tab = q["tab"];
            var filter = q["filter"];
            var folder = q["folder"].ToInt();
            var feed = q["feed"].ToInt();
            var tag = q["tag"].ToInt();
            var guid = q["guid"];

            var user = CurrentUser;

            if (user == null)
            {
                if (!guid.IsNullOrEmpty())
                {
                    user = UserService.GetUserByGuid(guid);
                    if (user == null)
                    {
                        System.Web.Security.FormsAuthentication.RedirectToLoginPage();
                        return null;
                    }
                }
                else
                {
                    System.Web.Security.FormsAuthentication.RedirectToLoginPage();
                    return null;
                }
            }

            var vm = new ArticleVM
            {
                User = user
            };

            var request = new ArticlesRequest { User = user, IsSingleArticleRequest = true, ArticleId = a };
            request.Filter = filter ?? "week";

            switch (tab)
            {
                case "month": request.Month = true; break;
                case "week": request.Week = true; break;
                case "folder": request.FolderId = folder; break;
                case "untaged": request.Untaged = true; break;
                case "favorite": request.Favorites = true; break;
                case "votes": request.Votes = true; break;
                case "myfeeds": request.MyFeeds = true; break;
            }

            request.FeedId = feed;
            request.TagId = tag;

            vm.Article = FeedService.GetArticles(request).FirstOrDefault();

            if (vm.Article == null)
            {
                //probably it was the last item from the list
                if (request.Week)
                {
                    q["tab"] = "month";
                    q["articleid"] = "";
                    q["next"] = "true";
                    return Next(q);
                }
                else if (request.Month)
                {
                    q["tab"] = "votes";
                    q["articleid"] = "";
                    q["next"] = "true";
                    return Next(q);
                }
                else if (!q["next"].ToBool())
                {
                    q["tab"] = "week";
                    q["articleid"] = "";
                    q["next"] = "true";
                    return Next(q);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            vm.Query = q;
            return View("Article", vm);
        }

        [HttpPost]
        public ActionResult Flag(int id)
        {
            var article = FeedService.GetArticle(id);
            if (article == null) return Json(false);

            if (article.FlaggedBy.Contains(CurrentUser.Id)) return Json(false);
            article.FlaggedBy.Add(CurrentUser.Id);

            article.Flagged = article.FlaggedBy.Count >= 3 || CurrentUser.IsAdmin; ;

            FeedService.UpdateArticle(article);

            if (article.Flagged && article.FlaggedBy.Count > 0)
            {
                IISTaskManager.Run(() =>
                {
                    var userToAddRep = article.FlaggedBy.First();
                    var user = UserService.GetUser(userToAddRep);
                    if (user != null)
                    {
                        user.Reputation += 2;
                        UserService.UpdateUser(user);
                    }
                });
            }

            return Json(true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIfUrlIsOk(int articleId)
        {
            var ok = true;
            var metaKey = "x-frame-options-allowed";

            var article = FeedService.GetArticle(articleId);
            var alreadyChecked = article.GetMetadataValue<bool?>(metaKey);
            if (alreadyChecked.HasValue) return Json(alreadyChecked);

            try
            {
                var url = article.Url;
                var request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "HEAD";
                request.Headers["Accept-Encoding"] = "gzip,deflate";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.KeepAlive = true;
                request.Timeout = 10000;
                request.MaximumAutomaticRedirections = 3;
                request.MaximumResponseHeadersLength = 1024 * 64 * 64;
                if (request != null)
                {
                    var response = request.GetResponse() as HttpWebResponse;
                    var headers = response.Headers;
                    var xFrameOptions = string.Empty;

                    if (response != null)
                    {
                        response.Close();
                    }

                    if (response.Headers != null)
                    {
                        xFrameOptions = response.Headers["X-Frame-Options"];
                    }
                    if (!xFrameOptions.IsNullOrEmpty())
                    {
                        if (xFrameOptions == "deny" ||
                            xFrameOptions == "SAMEORIGIN" ||
                            !xFrameOptions.Contains("rssheap"))
                        {
                            ok = false;
                            FeedService.FlagArticle(articleId);
                        }
                    }
                }
            }
            catch { }

            article.AddMetadata(metaKey, ok);
            article.SaveMetadata();
            return Json(ok);
        }

        [HttpPost]
        public ActionResult IgnoreFeed(int feedId)
        {
            FeedService.InsertFeedAsIgnored(feedId, CurrentUser);
            if (CurrentUser.MyFeeds.Any(f => f.Id == feedId))
            {
                CurrentUser.MyFeeds = CurrentUser.MyFeeds.Where(f => f.Id != feedId).ToList();
                UserService.UpdateUser(CurrentUser);
            }
            return Json(true);
        }

        [HttpPost]
        public ActionResult IgnoreArticle(int articleId)
        {
            FeedService.InsertArticleAsIgnored(articleId, CurrentUser);
            return Json(true);
        }

        public ActionResult NotFound()
        {
            Mail.SendMeAnEmail("NotFound", Request.RawUrl);
            return View();
        }

        [AllowAnonymous]
        public ActionResult ShortUrl(string shorturl)
        {
            var article = FeedService.GetArticle(shorturl);
            if (article == null)
                return NotFoundResult();

            IISTaskManager.Run(() =>
                {
                    ActionExtensions.TryAction(() =>
                    {
                        if (Request.Url.Host.Contains("rssheap"))
                        {
                            var tracker = new PageviewTracker("UA-51717870-1", "rssheap.com");
                            tracker.Parameters.DocumentPath = "/a/" + shorturl;
                            tracker.Parameters.DocumentTitle = article.Name;
                            tracker.Parameters.DocumentReferrer = Request.UrlReferrer?.AbsolutePath;
                            tracker.Parameters.UserLanguage =
                                (Request.UserLanguages != null && Request.UserLanguages.Count() > 0)
                                ? Request.UserLanguages.First() : null;
                            tracker.Parameters.UserAgentOverride = HttpUtility.UrlEncode(Request.UserAgent);
                            tracker.Parameters.IPOverride = Request.UserHostAddress;
                            tracker.Send();
                        }

                        if (CurrentUser != null)
                        {
                            IncreaseViewsCount(article.Id, CurrentUser.Id);
                        }
                    });
                });

            return new TemporaryRedirectResult(article.Url);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import()
        {
            var file = Request.Files.Count > 0 ? Request.Files[0] : null;
            if (file != null && file.ContentLength == 0) file = null;
            var url = Request["url"];

            var imported = false;

            if (file == null && url.IsNullOrEmpty())
                return RedirectToAction("Index", "Home");

            if (file != null)
                imported = ImportFile(file);

            if (!url.IsNullOrEmpty())
            {
                imported = ImportUrl(url, null);
                if (!imported)
                {
                    TempData["message"] = "The url '" + url + "' does not have publicly available rss feed!";
                }
            }

            if (imported)
            {
                TempData["message"] = "Thank you for importing feeds, they should be available in couple of minutes (if you uploaded OPML file it can take up to 15 mins depending on the file size). Your feeds will be avilable in My Feeds section.";
                TempData["ok"] = true;
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult Newfolder(string name)
        {
            try
            {
                if (name.IsNullOrEmpty()) return Json(new { error = "Name can not be empty" });
                if (name.Length > 100) return Json(new { error = "Name can not have more than 100 characters" });

                var folder = UserService.GetUserFolder(CurrentUser.Id, name);
                if (folder != null) return Json(new { error = "That folder already exists, try a different name" });

                var newFolder = UserService.InsertUserFolder(CurrentUser.Id, new Folder
                {
                    UserId = CurrentUser.Id,
                    Name = name
                });
                CurrentUser.Folders.Add(newFolder);
                return Json(new { id = newFolder.Id });
            }
            catch
            {
                return Json("There has been an error, please try again in a couple of minutes");
            }

        }

        [HttpPost]
        public ActionResult DeleteFolder(int Id)
        {
            var folder = UserService.GetUserFolder(CurrentUser.Id, Id);
            if (folder == null) return Json("We couldn't find that folder");

            UserService.DeleteUserFolder(CurrentUser.Id, folder.Id);
            CurrentUser.Folders = CurrentUser.Folders.Where(f => f.Id != folder.Id).ToList();
            return Json("ok");
        }

        [HttpPost]
        public ActionResult UserFolders(int feedId)
        {
            var foldersToAdd = UserService.GetUserFolderAvailableForFeed(CurrentUser.Id, feedId);
            var foldersToRemove = UserService.GetUserFoldersForFeed(CurrentUser.Id, feedId);
            return Json(new { toadd = foldersToAdd, toremove = foldersToRemove });
        }

        [HttpPost]
        public ActionResult AddToFolder(int folderId, int feedId)
        {
            var folder = UserService.GetUserFolder(CurrentUser.Id, folderId);
            if (folder == null) return Json("Folder does not exist");
            UserService.InsertUserFeedFolder(CurrentUser.Id, feedId, folder.Id);
            return Json("ok");
        }

        [HttpPost]
        public ActionResult RemoveFromFolder(int folderId, int feedId)
        {
            UserService.DeleteUserFeedFromFolder(CurrentUser.Id, folderId, feedId);
            return Json("ok");
        }

        private bool ImportFile(HttpPostedFileBase file)
        {
            if (file == null) return false;
            var filePath = Server.MapPath("//OPML//" + CurrentUser.Id + " " + DateTime.Now.ToFileTime() + " " + file.FileName);
            var fileName = Path.GetFileName(filePath);
            file.SaveAs(filePath);
            FeedService.InsertOPML(CurrentUser.Id, fileName);

            return true;
        }

        private bool ImportUrl(string htmlUrl, string rssUrl)
        {
            if (!htmlUrl.IsNullOrEmpty() && !htmlUrl.ToLower().StartsWith("http") && !htmlUrl.ToLower().StartsWith("https") && !htmlUrl.ToLower().StartsWith("www"))
            {
                htmlUrl = "http://" + htmlUrl;
            }

            try
            {
                var urls = new List<string>();
                if (rssUrl.IsNullOrEmpty() && htmlUrl.IsNullOrEmpty()) return false;
                if (!htmlUrl.IsNullOrEmpty() && !htmlUrl.StartsWith("http://") && !htmlUrl.StartsWith("https")) htmlUrl = "http://" + rssUrl;

                var exsFeed = FeedService.GetFeedBySiteUrl(!htmlUrl.IsNullOrEmpty() ? htmlUrl : rssUrl);
                if (exsFeed != null)
                {
                    var subscribe = true;// !Request["subscribe"].IsNullOrEmpty();
                    var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == exsFeed.Id);

                    if (userFeed == null)
                    {
                        userFeed = new UserFeed
                        {
                            FeedId = exsFeed.Id,
                            UserId = CurrentUser.Id,
                            Subscribed = subscribe,
                            Submited = true
                        };
                        userFeed.Id = UserService.InsertUserFeed(userFeed);
                        CurrentUser.MyFeeds.Add(userFeed);
                        CurrentUser.Reputation += 3;
                        UserService.UpdateUser(CurrentUser);
                    }
                    return true;
                }

                try
                {
                    var wc = new WebClient
                    {
                        Encoding = Encoding.UTF8
                    };
                    wc.Headers["User-Agent"] = "www.rssheap.com";
                    var str = wc.DownloadString(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl).Replace("media:thumbnail", "media");  //mashable fix

                    var reader = XmlReader.Create(new StringReader(str));

                    if (reader.IsXmlFeed())
                    {
                        urls.Add(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl);
                    }
                    else
                    {
                        //it's not an xml, try to find it from the tag
                        var request = FeedService.CreateRequest(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl);
                        request.Timeout = 5000;
                        var response = (HttpWebResponse)request.GetResponse();
                        try
                        {
                            var html = new HtmlDocument();
                            html.LoadHtml(new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd());
                            response.Close();

                            var links = html.DocumentNode
                                            .Descendants()
                                            .Where(l => l.Name == "link" &&
                                                        l.Attributes["rel"] != null && !l.Attributes["rel"].Value.IsNullOrEmpty() && l.Attributes["rel"].Value.ToLower() == "alternate" &&
                                                        l.Attributes["type"] != null && !l.Attributes["type"].Value.IsNullOrEmpty() && l.Attributes["type"].Value.Contains("xml") &&
                                                        l.Attributes["href"] != null && !l.Attributes["href"].Value.IsNullOrEmpty() && !l.Attributes["href"].Value.ToLower().Contains("comment"))
                                            .ToList();

                            if (links.Count > 1)
                            {
                                //try to find atom first
                                var temp = links.Where(l => l.Attributes["type"].Value.ToLower() == "application/atom+xml");
                                if (temp.Count() > 0)
                                {
                                    links = temp.ToList();
                                }
                                else
                                {
                                    temp = links.Where(l => l.Attributes["type"].Value.ToLower() == "application/rss+xml");
                                    if (temp.Count() > 0)
                                    {
                                        links = temp.ToList();
                                    }
                                    else
                                    {
                                        links = new List<HtmlNode> { links.First() };
                                    }
                                }
                            }

                            urls = links.Select(l => l.Attributes["href"].Value)
                                        .ToList();

                            if (urls.Count > 1) //usualy you get main feed and post feed, ignore the post feed
                            {
                                urls = urls.Where(u => !u.Contains(rssUrl)).ToList();
                            }

                        }
                        catch { }
                    }
                    if (urls.Count == 0) return false;

                    foreach (var u in urls)
                    {
                        var url = u;
                        if (url.StartsWith("/"))
                            url = new Uri(!htmlUrl.IsNullOrEmpty() ? htmlUrl : rssUrl).GetLeftPart(UriPartial.Authority) + url;

                        var rFeed = FeedService.GetRemoteFeed(new Feed
                        {
                            Url = url,
                            SiteUrl = htmlUrl
                        }, timeout: 3000);

                        if (rFeed != null)
                        {
                            if (!rFeed.Name.IsNullOrEmpty() && rFeed.Name.ToLower().Contains("comment")) continue;

                            rFeed.Public = CurrentUser.IsAdmin;

                            var blogUrl = rFeed.SiteUrl;
                            if (blogUrl.IsNullOrEmpty())
                                blogUrl = rFeed.Url;

                            var lFeed = FeedService.GetFeedBySiteUrl(blogUrl);
                            int feedId = lFeed == null ? FeedService.InsertFeed(rFeed) : lFeed.Id;

                            var subscribe = !Request["subscribe"].IsNullOrEmpty();
                            var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == feedId);

                            if (userFeed == null)
                            {
                                userFeed = new UserFeed
                                {
                                    FeedId = feedId,
                                    UserId = CurrentUser.Id,
                                    Subscribed = subscribe,
                                    Submited = true
                                };
                                userFeed.Id = UserService.InsertUserFeed(userFeed);
                                CurrentUser.MyFeeds.Add(userFeed);
                                CurrentUser.Reputation += 3;
                                UserService.UpdateUser(CurrentUser);
                            }
                            return true;
                        }
                    }
                }
                catch
                {

                }
            }
            catch
            {
            }
            return false;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void SubscribeToTag(int tagId, string url)
        {
            var tag = FeedService.GetTag(tagId);
            if (tag != null && !CurrentUser.FavoriteTagIds.Any(t => t == tagId))
            {
                CurrentUser.Tags.Add(tag);
                CurrentUser.FavoriteTagIds.Add(tagId);
                UserService.UpdateUser(CurrentUser);
                Redis.AddUserTag(CurrentUser, tagId, CurrentUser.IgnoredTagIds);
            }
            Response.Redirect(url, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void UnsubscribeFromTag(int tagId, string url)
        {
            if (CurrentUser.FavoriteTagIds.Any(t => t == tagId))
            {
                CurrentUser.FavoriteTagIds.Remove(tagId);
                CurrentUser.Tags = CurrentUser.Tags.Where(t => t.Id != tagId).ToList();
                UserService.UpdateUser(CurrentUser);
                Redis.RemoveUserTag(CurrentUser, tagId);
            }
            Response.Redirect(url, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void SubscribeToFeed(int feedId, string url)
        {
            var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == feedId);
            if (userFeed == null)
            {
                userFeed = new UserFeed
                {
                    UserId = CurrentUser.Id,
                    FeedId = feedId,
                    Subscribed = true
                };
                userFeed.Id = UserService.InsertUserFeed(userFeed);
                CurrentUser.MyFeeds.Add(userFeed);
            }
            else if (!userFeed.Subscribed)
            {
                userFeed.Subscribed = true;
                userFeed.Ignored = false;
                UserService.UpdateUserFeed(userFeed);
            }

            if (!url.IsNullOrEmpty())
                Response.Redirect(url, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void UnsubscribeFromFeed(int feedId, string url)
        {
            var userFeed = CurrentUser.MyFeeds.Find(f => f.FeedId == feedId);
            if (userFeed != null)
            {
                userFeed.Subscribed = false;
                userFeed.Ignored = false;
                UserService.UpdateUserFeed(userFeed);
            }
            if (!url.IsNullOrEmpty())
                Response.Redirect(url, true);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public void IncreaseViewsCount(int articleId, int userId)
        {
            var cookie = Request.Cookies["article"];
            var expirationTime = DateTime.Now.AddMinutes(2);
            if (cookie == null)
            {
                FeedService.UpdateArticleAsRead(userId, articleId);

                cookie = new HttpCookie("article", articleId.ToString())
                {
                    Expires = expirationTime
                };
                Response.Cookies.Add(cookie);
            }
            else
            {
                var cookieValue = cookie.Value;
                if (!cookieValue.ToCommaSeparatedListOfIds().Contains(articleId))
                {
                    FeedService.UpdateArticleAsRead(userId, articleId);
                    cookieValue += "," + articleId;
                    cookie.Value = cookieValue;
                    cookie.Expires = expirationTime;
                    Response.Cookies.Add(cookie);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void UpVote(int articleId)
        {
            FeedService.UpVote(CurrentUser, articleId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void DownVote(int articleId)
        {
            FeedService.DownVote(CurrentUser, articleId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void AddToFavorites(int articleId)
        {
            FeedService.InsertFavoriteArticle(CurrentUser, articleId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void RemoveFromFavorites(int articleId)
        {
            FeedService.DeleteFavoriteArticle(CurrentUser, articleId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuggestTag(int articleId, string name)
        {
            var response = FeedService.SuggestTag(articleId, name, CurrentUser);
            if (response != null && response.Approved)
                IISTaskManager.Run(() => Redis.AddArticleTag(response.Article, response.TagId));
            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectTag(int articleId, string tagName)
        {
            var response = FeedService.RejectTag(articleId, tagName, CurrentUser);
            if (response != null && response.Approved)
                Redis.RemoveArticleTag(response.Article, response.TagId);
            return Json(true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveTag(string tagName, int articleId)
        {
            var tag = FeedService.GetArticleTag(tagName, articleId);
            if (tag.CreatedBy != CurrentUser.Id)
            {
                if (tag.Approved && !CurrentUser.CanRemoveEntryTag) return Json(false);
            }

            FeedService.DeleteArticleTag(articleId, tagName);
            return Json(true);
        }
    }
}
