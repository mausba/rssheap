using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Core.Services;
using MvcWeb.Controllers;

namespace Web.Code.ActionFilters
{
    public class RequiresPRO : ActionFilterAttribute
    {
        private PaymentService PaymentService = new PaymentService();
        private UserService UserService = new UserService();

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.Controller as _BaseController;
            if (controller != null)
            {
                var user = controller.CurrentUser;
                var guid = HttpContext.Current.Request["guid"];
                if (user == null && !guid.IsNullOrEmpty())
                {
                    user = UserService.GetUserByGuid(guid);
                }

                var isPro = PaymentService.IsPro(user);
                if (!isPro)
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary { { "Controller", "Home" }, { "Action", "Pro" } });
                }
                else
                {
                    base.OnActionExecuting(filterContext);
                }
            }
            else
            {
                base.OnActionExecuting(filterContext);
            }
        }
    }
}