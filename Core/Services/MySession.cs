using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Core.Services
{
    public static class MySession
    {
        public static void AddVisitedArticle(int articleId)
        {
            if (HttpContext.Current == null) return;
            var session = HttpContext.Current.Session;
            var articles = session["visitedarticles"] as List<int>;
            if (articles == null) articles = new List<int>();

            if (!articles.Any(a => a == articleId))
                articles.Add(articleId);

            if (articles.Count > 5)
                articles.Remove(articles.First());

            session["visitedarticles"] = articles;
        }

        public static List<int> GetVisitedArticles()
        {
            if (HttpContext.Current == null) return new List<int>();
            var session = HttpContext.Current.Session;

            if (session == null) return new List<int>();
            var articles = session["visitedarticles"] as List<int>;

            if (articles == null) return new List<int>();
            return articles;
        }
    }
}