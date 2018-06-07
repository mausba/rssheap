using Core.Services;
using MvcWeb.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class MailController : _BaseController
    {
        public ActionResult Articles()
        {
            return View();
        }

        public ActionResult Unsubscribe()
        {
            var user = UserService.GetUserByGuid(Request["guid"]);
            if (user == null) return RedirectToAction("Articles", "Home");

            return View(user);
        }

        [HttpPost]
        public ActionResult Unsubscribe(string guid)
        {
            var user = UserService.GetUserByGuid(guid);
            if (user == null) return RedirectToAction("Articles", "Home");

            user.Subscribed = false;
            UserService.UpdateUser(user);
            return View(user);
        }
    }
}