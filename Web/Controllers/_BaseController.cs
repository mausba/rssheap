using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Models;
using Core.Services;
using System.Web.Security;
using System.Web.Caching;
using Core.Extensions;
using Core.Utilities;
using Core.Caching;

namespace MvcWeb.Controllers
{
    public class _BaseController : Controller
    {
        public FeedService FeedService = new FeedService();
        public UserService UserService = new UserService();
        public PaymentService PaymentService = new PaymentService();

        public User CurrentUser
        {
            get
            {
                if (HttpContext.User == null) return null;
                if (!HttpContext.User.Identity.IsAuthenticated) return null;

                var currentUser = UserService.GetUserCached(HttpContext.User.Identity.Name,
                    (user) =>
                    {
                        if (user != null)
                        {
                            user.Tags = FeedService.GetTags(user.FavoriteTagIds);
                            user.FavoriteTagIds = user.Tags.Select(t => t.Id).ToList();

                            if (user.FavoriteTagIds.Count > 0)
                            {
                                var likedTagIds = FeedService.GetTagsThatUserLikes(user.Id);
                                var likedTags = FeedService.GetTags(likedTagIds);
                                user.Tags.AddRange(likedTags);

                                user.FavoriteTagIds.AddRange(likedTagIds);
                                user.FavoriteTagIds = user.FavoriteTagIds.Distinct().Where(t => !user.IgnoredTagIds.Contains(t)).ToList();
                            }

                            user.IgnoredTags = FeedService.GetTags(user.IgnoredTagIds);
                            user.MyFeeds = UserService.GetUserFeeds(user.Id);
                            user.Folders = UserService.GetUserFolders(user.Id);
                        }
                    });

                ActionExtensions.TryAction(() =>
                {
                    var lastSeen3MinsAgo = (DateTime.Now - currentUser.LastSeen).TotalMinutes >= 3;
                    if (lastSeen3MinsAgo)
                    {
                        IISTaskManager.Run(() =>
                        {
                            currentUser.LastSeen = DateTime.Now;
                            UserService.UpdateLastSeen(currentUser);
                        });
                    }
                }, "Error in update LastSeen");

                return currentUser;
            }
        }

        public void ClearUserCache() => CacheClient.Default.Remove("user" + HttpContext.User.Identity.Name);
    }
}
