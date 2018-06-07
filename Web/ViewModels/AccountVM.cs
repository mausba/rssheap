using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Core.Models;

namespace Web.ViewModels
{
    public class AccountVM
    {
        public User User { get; set; }
        public string Tab { get; set; }

        public List<Feed> Feeds { get; set; }

        public List<Article> TaggedArticles { get; set; }
        public List<UserTaggedArticle> UserTags { get; set; }
        public List<Tag> Tags { get; set; }

        public List<Article> IgnoredArticles { get; set; }
    }
}