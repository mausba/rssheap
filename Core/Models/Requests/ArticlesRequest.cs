using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Models.Requests
{
    public class Day
    {
        public DateTime Date { get; set; }
    }

    public class ArticlesRequest
    {
        public ArticlesRequest()
        {
            IgnoredArticleIds = new List<int>();
        }

        public bool IncludeArticleBody { get; set; }

        public User User { get; set; }
        public bool Week { get; set; }
        public bool Month { get; set; }
        public Day Day { get; set; }

        public bool Untaged { get; set; }
        public bool Votes { get; set; }
        public bool Favorites { get; set; }
        public bool MyFeeds { get; set; }

        public bool IsSingleArticleRequest { get; set; }

        public int FolderId { get; set; }

        public int ArticleId { get; set; }

        public string Filter { get; set; }

        public List<int> IgnoredArticleIds { get; set; }

        public string SearchQuery { get; set; }

        public int FeedId { get; set; }
        public Feed Feed { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }

        private int PageInternal;
        public int Page { get { if (PageInternal == 0) return 1; else return PageInternal; } set { PageInternal = value; } }
        public int PageSize { get; set; }

        public bool DoNotFilterByTags { get; set; }

        public bool IsFromMobile { get; set; }

        public int AppVersion { get; set; }
    }
}
