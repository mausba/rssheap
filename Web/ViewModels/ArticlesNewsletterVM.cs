using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.ViewModels
{
    public class ArticlesNewsletterVM
    {
        public ArticlesNewsletterVM()
        {
            WeekArticles = new List<Article>();
            AllTimeArticles = new List<Article>();
            UntagedArticles = new List<Article>();
        }

        public List<Article> WeekArticles { get; set; }
        public List<Article> AllTimeArticles { get; set; }
        public List<Article> UntagedArticles { get; set; }
    }
}