using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Models.Responses
{
    public class TagResponse
    {
        public Article Article { get; set; }
        public int TagId { get; set; }
        public string TagName { get; set; }
        public bool Approved { get; set; }
        public string Error { get; set; }
    }
}
