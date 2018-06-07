using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class UserTaggedArticle
    {
        public int ArticleId { get; set; }
        public int TagId { get; set; }
        public bool Approved { get; set; }
    }
}
