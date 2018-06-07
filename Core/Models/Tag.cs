using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace Core.Models
{
    [DebuggerDisplay("{Id} {Name}")]
    public class Tag
    {
        public Tag()
        {
            ApprovedBy = new List<int>();
            RejectedBy = new List<int>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ArticlesCount { get; set; }
        public int SubscribersCount { get; set; }
        public bool Active { get; set; }
        public int SynonimTagId { get; set; }
        public List<int> SimilarTagIds { get; set; }

        public bool Approved { get; set; }
        public int SubmittedBy { get; set; }
        public List<int> ApprovedBy { get; set; }
        public List<int> RejectedBy { get; set; }

        public int CreatedBy
        {
            get { return ApprovedBy.Count > 0 ? ApprovedBy.First() : 0; }
        }

        public DateTime Created { get; set; }

        public string PrettyUrl { get { return "/tags/" + HttpUtility.UrlEncode(Name); } }
        public bool MatchTitleOnly { get; set; }
    }
}
