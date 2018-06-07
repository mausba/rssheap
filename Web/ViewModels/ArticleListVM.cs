using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcWeb.ViewModels
{
    public class ArticleListVM
    {
        public ArticleListVM()
        {
            Articles = new List<Article>();
        }
        public List<Article> Articles { get; set; }

        public static ArticleListVM ToArticleListVM(Article article)
        {
            return new ArticleListVM
            {
                Articles = new List<Article> { article }
            };
        }
    }
}