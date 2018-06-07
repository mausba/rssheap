using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Web.Code.ActionFilters
{
    public class SpamProtectionAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            filterContext.Controller.ValidateRequest = false;
            long timestamp = long.MaxValue;

            if (Int64.TryParse(filterContext.RequestContext.HttpContext.Request.Unvalidated.Form["SpamProtectionTimeStamp"], out timestamp))
            {
                long currentTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;

                if (currentTime <= timestamp + 1)
                {
                    throw new HttpException("Invalid form submission.");
                }
            }
            else
            {
                throw new HttpException("Invalid form submission.");
            }

            if (!filterContext.RequestContext.HttpContext.Request.Unvalidated.Form["surname"].IsNullOrEmpty())
            {
                throw new HttpException("Invalid form submission");
            }
        }
    }
}