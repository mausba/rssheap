using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Core.Models
{
    [DebuggerDisplay("FeedId: {FeedId} Submitted: {Submited}")]
    public class UserFeed
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FeedId { get; set; }
        public bool Submited { get; set; }
        public bool Ignored { get; set; }
        public bool Subscribed { get; set; }
    }
}
