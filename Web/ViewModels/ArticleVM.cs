using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Core.Models;
using System.Collections.Specialized;

namespace MvcWeb.ViewModels
{
    public class ArticleVM
    {
        public ArticleVM()
        {
            RelatedArticles = new List<Article>();
        }

        public User User { get; set; }
        public Article Article { get; set; }

        public List<Article> RelatedArticles { get; set; }
        public NameValueCollection Query { get; set; }
    }
}