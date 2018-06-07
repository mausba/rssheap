using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Core.Models;
using Core.Models.Requests;

namespace MvcWeb.ViewModels
{
    public class TagsVM
    {
        public TagsVM()
        {
            Tags = new List<Tag>();
        }

        public TagsRequest Request { get; set; }

        public List<Tag> Tags { get; set; }

        public User CurrentUser { get; set; }

        public bool ShowNextPage { get { return Tags.Count >= PageSize; } }
        public bool ShowPreviousPage { get { return Page > 1; } }

        public int Page { get { return Request != null ? Request.Page : 0; } }
        public int PageSize { get { return Request != null ? Request.PageSize : 0; } }
    }
}