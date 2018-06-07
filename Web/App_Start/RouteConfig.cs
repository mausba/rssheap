using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "privacy",
                "privacy",
                new { controller = "Home", action = "Privacy" }
                );

            routes.MapRoute(
                "cookie-policy",
                "cookie-policy",
                new { controller = "Home", action = "CookiePolicy" }
                );

            routes.MapRoute(
                "terms",
                "terms",
                new { controller = "Home", action = "Terms" }
                );

            routes.MapRoute(
                "atom",
                "atom",
                new { controller = "Home", action = "atom" }
                );

            routes.MapRoute(
                "tags",
                "tags",
                new { controller = "Tag", action = "Tags" }
                );

            routes.MapRoute(
                "error",
                "error",
                new { controller = "Home", action = "Error" }
                );

            routes.MapRoute(
                "notfound",
                "notfound",
                new { controller = "Home", action = "NotFound" }
                );

            routes.MapRoute(
                "register",
                "register",
                new { controller = "Account", action = "Register" }
                );

            routes.MapRoute(
                "login",
                "login",
                new { controller = "Account", action = "Login" }
                );

            routes.MapRoute(
                "articles",
                "articles",
                new { controller = "Home", action = "Articles" }
                );

            routes.MapRoute(
                "article-shorturl",
                "a/{shorturl}",
                new { controller = "Article", action = "ShortUrl", name = UrlParameter.Optional }
                );

            routes.MapRoute(
                "article",
                "articles/{id}/{name}",
                new { controller = "Article", action = "Article", name = UrlParameter.Optional },
                new { id = @"\d+" }
                );

            routes.MapRoute(
                "single-article",
                "article",
                new { controller = "Article", action = "Article" }
                );

            routes.MapRoute(
                "tag",
                "tags/{name}",
                new { controller = "Home", action = "Tag" }
                );

            routes.MapRoute(
                "feed-favicon",
                "feed/favicon/{id}",
                new { controller = "Article", action = "Favicon" },
                new { id = @"\d+" }
                );

            routes.MapRoute(
                "feed",
                "feeds/{feedId}/{feedName}",
                new { controller = "Home", action = "Feed", feedName = UrlParameter.Optional },
                new { feedId = @"\d+" }
                );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
           
        }
    }
}
