using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Web.Code.ActionResults
{
    public class TemporaryRedirectResult : ActionResult
    {
        public string Url { get; set; }

        public TemporaryRedirectResult(string url)
        {
            this.Url = url;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.StatusCode = 302;
            context.HttpContext.Response.RedirectLocation = this.Url;
            context.HttpContext.Response.End();
        }
    }
}