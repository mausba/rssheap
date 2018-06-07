using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Models.Requests;
using MvcWeb.Controllers;
using MvcWeb.ViewModels;

namespace Web.Controllers
{
    [Authorize]
    public class TagController : _BaseController
    {
        public const int PageSize = 100;

        public ActionResult Tags(int page = 0, string tab = null, string tag = null)
        {
            var request = new TagsRequest();
            request.SearchQuery = tag;
            request.PageSize = PageSize;
            request.Page = page;

            switch (tab)
            {
                case "name": request.Name = true; break;
                case "new": request.New = true; break;
                default: request.Popular = tag.IsNullOrEmpty(); break;
            }

            return GetTagsView(request);
        }

        private ActionResult GetTagsView(TagsRequest request)
        {
            var tags = FeedService.GetTags(request);
            return View("Tags", new TagsVM
            {
                CurrentUser = CurrentUser,
                Request = request,
                Tags = tags
            });
        }
    }
}