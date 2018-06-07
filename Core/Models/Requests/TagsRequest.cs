using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Requests
{
    public class TagsRequest
    {
        public bool Popular { get; set; }
        public bool Name { get; set; }
        public bool New { get; set; }
        public string SearchQuery { get; set; }

        private int PageInternal;
        public int Page { get { if (PageInternal == 0) return 1; else return PageInternal; } set { PageInternal = value; } }
        public int PageSize { get; set; }
    }
}
