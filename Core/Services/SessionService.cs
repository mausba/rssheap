using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Core.Services
{
    public static class SessionService
    {
        public static void AddVisitedArticle(int userId, int articleId)
        {
            var articles = HttpContext.Current.Session["readarticles" + userId] as List<int>;
            if (articles == null) articles = new List<int>();

            if (!articles.Any(a => a == articleId)) articles.Add(articleId);

            if (articles.Count >= 5) articles.Remove(articles.First());
            HttpContext.Current.Session["readarticles" + userId] = articles;
        }

        public static List<int> GetVisitedArticles(int userId)
        {
            var articles = HttpContext.Current.Session["readarticles" + userId] as List<int>;
            if (articles == null) articles = new List<int>();
            return articles;
        }
    }
}
