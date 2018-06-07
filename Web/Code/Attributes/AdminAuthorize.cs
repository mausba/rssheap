using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Services;
using MvcWeb.Controllers;

namespace Web.Code.Attributes
{
    public class AdminAuthorize : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.User.Identity == null) return false;
            if (!httpContext.Request.IsAuthenticated) return false;

            var user = new UserService().GetUserCached(httpContext.User.Identity.Name, null);
            if (user == null) return false;

            return user != null ? user.IsAdmin : false;
        }
    }
}