using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Enums;

namespace Core.Models
{
    public class FeedEntryActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int FollowingUserId { get; set; }
        public string FollowingUserName { get; set; }
        public int FeedId { get; set; }
        public string FeedName { get; set; }
        public int EntryId { get; set; }
        public string EntryName { get; set; }
        public EntryActions EntryAction { get; set; }
        public DateTime Date { get; set; }
        public string Note { get; set; }
    }
}
