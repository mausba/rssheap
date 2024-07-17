using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Services;
using MvcWeb.ViewModels;
using Core.Models.Requests;
using Core.Utilities;
using System.Globalization;
using Web.Code.ActionFilters;
using System.ServiceModel.Syndication;
using System.Xml;
using Core.Caching;

using Web.ViewModels;
namespace MvcWeb.Controllers
{
    [Authorize]
    [NonSSL]
    public class HomeController : _BaseController
    {
        public readonly int PageSize = 100;
        public readonly int NumberOfFreeSearches = 2;

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                if (CurrentUser.Tags.Count > 0)
                    return RedirectToAction("Articles", "Home");

                return RedirectToAction("Tags", "Tag");
            }

            return View("HomePage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [ValidateInput(false)]
        [SpamProtection]
        public ActionResult Index(string name, string email, string body)
        {
            if (body.IsNullOrEmpty()) return View();

            Func<string, string, string> getRowFunc = (title, value) =>
            {
                return "<tr><td>" + title + " </td><td>" + value + "</td></tr>";
            };

            var html = "<table>";
            html += getRowFunc("Name", name);
            html += getRowFunc("Email", email);
            html += getRowFunc("Body", body);
            html += CurrentUser != null ? getRowFunc("UserId", CurrentUser.Id.ToString()) : string.Empty;
            html += "</table>";

            bool sent = Mail.SendMeAnEmail("New contact form on website from " + email, html, name, email);
            ViewBag.Message = sent ? "Thank you for sending me a message"
                                   : "We couldn't send an email, try again or send an email directly to mail@rssheap.com";

            return View("Homepage");
        }

        [AllowAnonymous]
        public ActionResult CookiePolicy()
        {
            return View();
        }

        public ActionResult Articles(int page = 0, string tab = null, string all = null, string q = null, int folder = 0, string filter = null)
        {
            var request = new ArticlesRequest { User = CurrentUser, Page = page, PageSize = PageSize };
            bool includeAll = bool.TryParse(all, out includeAll) ? includeAll : false;
            request.DoNotFilterByTags = includeAll;
            request.SearchQuery = q;
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
                default: request.Week = q.IsNullOrEmpty(); break;
            }

            return GetIndexView(request);
        }

        private ActionResult GetIndexView(ArticlesRequest request)
        {
            var hvm = new EntriesVM();
            hvm.Request = request;

            if (!request.SearchQuery.IsNullOrEmpty())
            {
                var isPro = PaymentService.IsPro(CurrentUser);
                if (!isPro)
                {
                    var numberOfSearchesAlready = FeedService.GetUserSearchesForDate(CurrentUser);
                    if (numberOfSearchesAlready >= NumberOfFreeSearches)
                    {
                        hvm.ExcededFreeSearchCount = true;
                    }
                }
                FeedService.AddSearch(request.SearchQuery, CurrentUser);
            }

            if (!hvm.ExcededFreeSearchCount)
            {
                hvm.Articles = FeedService.GetArticles(request);
            }

            SaveRequestToCookie();

            hvm.CurrentUser = CurrentUser;
            return View("Index", hvm);
        }

        [AllowAnonymous]
        public ActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Terms()
        {
            return View();
        }

        [RequiresPRO]
        [AllowAnonymous]
        public void Atom(string guid, string tab = null, string q = null, int folder = 0)
        {
            var user = UserService.GetUserByGuid(guid);
            if (user == null) return;

            var title = string.Empty;
            var link = string.Empty;

            var request = new ArticlesRequest { User = user, PageSize = PageSize };
            request.SearchQuery = q;
            request.IncludeArticleBody = true;
            request.User.HideVisitedArticles = true;
            request.Filter = "votes";
            switch (tab)
            {
                case "month":
                    request.Month = true;
                    title = "Month articles via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles?tab=month";
                    break;
                case "week":
                    request.Week = true;
                    title = "Week articles via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles";
                    break;
                case "folder":
                    request.FolderId = folder;
                    var folderObj = UserService.GetUserFolder(user.Id, folder);
                    if (folderObj != null)
                    {
                        title = "Articles in " + folderObj.Name + " folder via " + user.UserName + " in rssheap";
                        link = "http://rssheap.com?tab=folder&folder=" + folderObj.Id;
                    }
                    break;
                case "untaged":
                    request.Untaged = true;
                    title = "Untagged articles via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles?tab=untaged";
                    break;
                case "favorite":
                    request.Favorites = true;
                    title = "Favorite articles via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles?tab=favorite";
                    break;
                case "votes":
                    request.Votes = true;
                    title = "All articles by votes via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles?tab=votes";
                    break;
                case "myfeeds":
                    request.MyFeeds = true;
                    title = "My feeds articles by votes via " + user.UserName + " in rssheap";
                    link = "http://rssheap.com/articles?tab=myfeeds";
                    break;
                default:
                    {
                        request.Week = q.IsNullOrEmpty();
                        title = "Week articles via " + user.UserName + " in rssheap";
                        link = "http://rssheap.com/articles";
                        break;
                    }
            }

            var articles = FeedService.GetArticles(request);
            var items = new List<SyndicationItem>();
            foreach (var a in articles)
            {
                var si = new SyndicationItem();
                si.Id = a.Id.ToString();
                si.Links.Add(new SyndicationLink(new Uri("http://www.rssheap.com/a/" + a.ShortUrl)));
                si.Title = new TextSyndicationContent(a.Name);
                if (a.Published == DateTime.MinValue)
                    a.Published = DateTime.Now.AddYears(-2);
                si.LastUpdatedTime = a.Published;
                si.PublishDate = a.Published;
                si.Content = new TextSyndicationContent(a.Body, TextSyndicationContentKind.Html);

                items.Add(si);
            }

            var updated = DateTime.Now;
            if (items.Count > 0)
                updated = articles.OrderBy(i => i.Published).First().Published;
            var feed = new SyndicationFeed(title, string.Empty, new Uri(link), link, updated, items);
            feed.LastUpdatedTime = updated;

            Response.ContentType = "text/xml";

            // Return the feed's XML content as the response
            var feedWriter = XmlWriter.Create(Response.OutputStream);
            Atom10FeedFormatter atomFormatter = new Atom10FeedFormatter(feed);
            atomFormatter.WriteTo(feedWriter);
            feedWriter.Close();
        }

        [AllowAnonymous]
        public ActionResult Pro()
        {
            var user = CurrentUser;
            if (user != null && PaymentService.IsPro(user))
            {
                return RedirectToAction("Index", "Card");
            }
            return View();
        }

        public ActionResult GoBack()
        {
            var backUrl = GetRequestFromCookie();
            if (backUrl.IsNullOrEmpty()) return Index();
            Response.Redirect(backUrl + (backUrl.Contains("?") ? "&" : "?") + "back=true", true);
            return null;
        }

        [AllowAnonymous]
        public ActionResult Error()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult NotFound()
        {
            return View("404");
        }

        public ActionResult hideolderthan(string months, string date)
        {
            if (months == "date")
            {
                var d = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                if (d >= DateTime.Now.AddDays(-60))
                {
                    TempData["message"] = "Date should be at least 2 monts before today";
                    return RedirectToAction("Index");
                }
                else
                {
                    CurrentUser.HideOlderThan = d.ToString("dd-MM-yyyy");
                    UserService.UpdateUser(CurrentUser);
                    TempData["message"] = "You will no longer see articles published before " + d.ToLongDateString();
                    return RedirectToAction("Index");
                }
            }
            else
            {
                int m = int.TryParse(months, out m) ? m : 0;
                if (m > 0)
                {
                    CurrentUser.HideOlderThan = m.ToString();
                    UserService.UpdateUser(CurrentUser);
                    TempData["message"] = "You will no longer see articles published " + m + " months ago and older";
                    return RedirectToAction("Index");
                }
            }

            CurrentUser.HideOlderThan = null;
            UserService.UpdateUser(CurrentUser);
            TempData["message"] = "You will now see all the articles no matter when they were published";
            return RedirectToAction("Index");
        }

        public ActionResult Feed(int feedId = 0, string feedName = null, int page = 0, string filter = null)
        {
            var feed = FeedService.GetFeed(feedId);
            if (feed == null)
            {
                Response.StatusCode = 404;
                return RedirectToAction("Index");
            }

            return GetIndexView(new ArticlesRequest
            {
                FeedId = feedId,
                Feed = feed,
                Page = page,
                PageSize = PageSize,
                User = CurrentUser,
                Filter = filter ?? "all"
            });
        }

        public ActionResult Tag(string name, int page = 0, string filter = null)
        {
            var tag = FeedService.GetTag(name);
            if (tag == null)
            {
                Response.StatusCode = 404;
                return RedirectToAction("Index");
            }
            return GetIndexView(new ArticlesRequest
            {
                TagId = tag.Id,
                Tag = tag,
                User = CurrentUser,
                Page = page,
                PageSize = PageSize,
                Filter = filter ?? "week"
            });
        }

        public ActionResult searchtags(string q)
        {
            var tags = FeedService.GetTagsContaining(q, 5);
            return Json(tags.Select(t => t.Name), JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Stats()
        {
            return PartialView("HomePage/_Stats");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void hidevisited(FormCollection form)
        {
            var hide = form["hide"] != null;
            var url = form["url"];

            CurrentUser.HideVisitedArticles = hide;
            UserService.UpdateUser(CurrentUser);
            if (hide)
                Redis.HideVisitedArticles(CurrentUser.Id);
            else
                Redis.ShowVisitedArticles(CurrentUser.Id);

            Response.Redirect(url, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        [ValidateInput(false)]
        [SpamProtection]
        public ActionResult contact(string name, string email, string subject, string body)
        {
            if (body.IsNullOrEmpty()) return View();

            Func<string, string, string> getRowFunc = (title, value) =>
            {
                return "<tr><td>" + title + " </td><td>" + value + "</td></tr>";
            };

            var html = "<table>";
            html += getRowFunc("Name", name);
            html += getRowFunc("Email", email);
            html += getRowFunc("Subject", subject);
            html += getRowFunc("Body", body);
            if (CurrentUser != null)
                html += getRowFunc("UserId", CurrentUser.Id.ToString());
            html += "</table>";

            bool sent = Mail.SendMeAnEmail("New contact form on website from " + email, html, name, email);
            if (sent)
                ViewBag.Message = "Thank you for sending me a message";
            else
                ViewBag.Message = "We couldn't send an email, try again or send an email directly to dzlotrg@gmail.com";
            return View("About");
        }

        private void SaveRequestToCookie()
        {
            var queryString = HttpUtility.ParseQueryString(Request.QueryString.ToString());
            queryString.Remove("back");

            var url = Request.Path;
            if (queryString.Count > 0) url += "?" + queryString;

            var cookie = new HttpCookie("url", url);
            cookie.Expires = DateTime.Now.AddDays(2);
            Response.Cookies.Add(cookie);
        }

        private string GetRequestFromCookie()
        {
            var cookie = Request.Cookies["url"];
            if (cookie == null || cookie.Value.IsNullOrEmpty()) return null;
            return cookie.Value;
        }
    }
}
