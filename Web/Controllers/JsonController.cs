using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Core.Extensions;
using Core.Models;
using Core.Utilities;

namespace MvcWeb.Controllers
{
    public class JsonController : _BaseController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public void tweet(string guid)
        {
            var user = UserService.GetUserByGuid(guid);
            if (user == null) return;

            user.SharedOnTwitter = true;
            CurrentUser.SharedOnTwitter = true;
            UserService.UpdateUser(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public void facebook(string guid)
        {
            var user = UserService.GetUserByGuid(guid);
            if (user == null) return;

            user.SharedOnFacebook = true;
            CurrentUser.SharedOnFacebook = true;
            UserService.UpdateUser(user);
        }

        [HttpPost]
        public void jserror(string message, string url, string lineNumber)
        {
            Mail.SendMeAnEmail("Error in javascript", "message: " + message + "<br/> url: " + url + "<br/> lineNumber: " + lineNumber);
        }

        [HttpPost]
        public ActionResult DeleteAccount()
        {
            UserService.DeleteUser(CurrentUser.Id);
            FormsAuthentication.SignOut();
            return Json("ok");
        }

        [HttpPost]
        public ActionResult AddFavoriteOrIgnoredTag(string name, bool ignore)
        {
            if (name.IsNullOrEmpty()) return Json(false);
            name = name.Trim();
            if (name.IsNullOrEmpty()) return Json(false);
            var tagName = name.ToTagName();

            var tag = FeedService.GetTag(tagName);
            var userTags = ignore ? CurrentUser.IgnoredTagIds : CurrentUser.FavoriteTagIds;

            if (tag != null && !userTags.Contains(tag.Id))
            {
                if(ignore) 
                {
                    CurrentUser.IgnoredTags.Add(tag);
                    CurrentUser.IgnoredTagIds.Add(tag.Id);
                }
                else
                {
                    CurrentUser.Tags.Add(tag);
                    CurrentUser.FavoriteTagIds.Add(tag.Id);
                    ActionExtensions.TryAction(() =>
                        {
                            IISTaskManager.Run(() =>
                            {
                                tag.SubscribersCount++;
                                FeedService.UpdateTag(tag);
                            });
                        }, "Error in increasing tag subscribers count");
                }

                UserService.UpdateUser(CurrentUser);
                return Json(tag.Name, JsonRequestBehavior.AllowGet);
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult RemoveFavoriteOrIgnoredTag(string name, bool ignore)
        {
            if (name.IsNullOrEmpty()) return Json(false);
            name = name.Trim();
            if (name.IsNullOrEmpty()) return Json(false);
            var tagName = name.ToTagName();

            var tag = FeedService.GetTag(tagName);
            var userTags = ignore ? CurrentUser.IgnoredTagIds : CurrentUser.FavoriteTagIds;

            if (tag != null && userTags.Contains(tag.Id))
            {
                if (ignore)
                {
                    CurrentUser.IgnoredTags = CurrentUser.IgnoredTags.Where(t => t.Id != tag.Id).ToList();
                    CurrentUser.IgnoredTagIds = CurrentUser.IgnoredTagIds.Where(t => t != tag.Id).ToList();
                }
                else
                {
                    CurrentUser.Tags = CurrentUser.Tags.Where(t => t.Id != tag.Id).ToList();
                    CurrentUser.FavoriteTagIds = CurrentUser.FavoriteTagIds.Where(t => t != tag.Id).ToList();
                    ActionExtensions.TryAction(() =>
                        {
                            IISTaskManager.Run(() =>
                                {
                                    tag.SubscribersCount--;
                                    FeedService.UpdateTag(tag);
                                });
                        }, "Error in decreasing tag subscribers count");
                }

                UserService.UpdateUser(CurrentUser);
                return Json(tag.Name, JsonRequestBehavior.AllowGet);
            }

            return Json(false, JsonRequestBehavior.AllowGet);
        }
    }
}
