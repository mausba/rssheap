using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages.Html;
using Core.Models;
using Core.Enums;
using MvcWeb.Controllers;
using MvcWeb.ViewModels;
using System.Text.RegularExpressions;
using System.Web.WebPages;

namespace System.Web.Mvc
{
    public static class HtmlExtensions
    {

        public static Uri AddQuery(this Uri uri, string name, string value)
        {
            var ub = new UriBuilder(uri);
            var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);
            
            httpValueCollection.Remove(name);
            httpValueCollection.Remove("page");
            httpValueCollection.Remove("back");

            httpValueCollection.Add(name, value);
            ub.Query = httpValueCollection.ToString();

            return ub.Uri;
        }


        public static MvcHtmlString Script(this HtmlHelper htmlHelper, Func<object, HelperResult> template)
        {
            htmlHelper.ViewContext.HttpContext.Items["_script_" + Guid.NewGuid()] = template;
            return MvcHtmlString.Empty;
        }

        public static IHtmlString RenderScripts(this HtmlHelper htmlHelper)
        {
            foreach (object key in htmlHelper.ViewContext.HttpContext.Items.Keys)
            {
                if (key.ToString().StartsWith("_script_"))
                {
                    var template = htmlHelper.ViewContext.HttpContext.Items[key] as Func<object, HelperResult>;
                    if (template != null)
                    {
                        htmlHelper.ViewContext.Writer.Write(template(null));
                    }
                }
            }
            return MvcHtmlString.Empty;
        }

        public static IHtmlString _GetNextPage(this HtmlHelper helper, TagsVM tagsVM)
        {
            return GetUrl(tagsVM.Page + 1);
        }

        public static IHtmlString _GetNextPage(this HtmlHelper helper, EntriesVM entriesVM)
        {
            return GetUrl(entriesVM.Page + 1);
        }

        public static IHtmlString _GetIgnoreTags(this HtmlHelper helper)
        {
            var url = HttpContext.Current.Request.RawUrl;
            if (!url.Contains("all"))
                url += (url.Contains("?") ? "&" : "?") + "all=true";
            return new HtmlString(url);
        }

        public static IHtmlString _GetPreviousPage(this HtmlHelper helper, TagsVM tagsVM)
        {
            return GetUrl(tagsVM.Page - 1);
        }

        public static IHtmlString _GetPreviousPage(this HtmlHelper helper, EntriesVM entriesVM)
        {
            return GetUrl(entriesVM.Page - 1);
        }

        private static HtmlString GetUrl(int page)
        {
            var queryString = HttpUtility.ParseQueryString(HttpContext.Current.Request.QueryString.ToString());
            queryString.Remove("back");

            var url = HttpContext.Current.Request.Path;
            if (queryString.Count > 0) url += "?" + queryString;

            var regex = new Regex(@"page=\d+", RegexOptions.IgnoreCase);
            if (regex.IsMatch(url))
            {
                url = regex.Replace(url, "page=" + page.ToString());
            }
            else
            {
                url += url.Contains("?") ? "&" : "?";
                url += "page=" + page;
            }

            return new HtmlString(url);
        }
    }
}