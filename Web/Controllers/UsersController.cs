using Core.Models;
using MvcWeb.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Web.Code.Attributes;
using Web.ViewModels;

namespace Web.Controllers
{
    [AdminAuthorize]
    [Authorize]
    public class UsersController : _BaseController
    {
        [AdminAuthorize]
        public ActionResult Index(string option, string search)
        {
            var users = UserService.GetAllUsers();

            List<UserVM> model = users.Select(x => new UserVM
            {
                Id = x.Id,
                UserName = x.UserName,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                IsAdmin = x.IsAdmin,
                Created = x.Created,
                LastSeen = x.LastSeen,
            }).ToList();

            var filteredModel = FilterUsers(model, option, search);

            return View(filteredModel);
        }

        [AdminAuthorize]
        public ActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("Index", "Users");

            var user = UserService.GetUser(id.Value);
            var model = new UserVM
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsAdmin = user.IsAdmin,
                Created = user.Created,
                LastSeen = user.LastSeen,
            };

            if (model == null) return RedirectToAction("Index");

            var currentUser = CurrentUser;

            if (currentUser.IsAdmin == false) return RedirectToAction("Index", "Home");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
        public ActionResult Edit(UserVM user)
        {
            var userCurrent = CurrentUser;

            if (ModelState.IsValid)
            {
                var dbUser = new User
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsAdmin = user.IsAdmin,
                    Created = user.Created,
                    LastSeen = user.LastSeen,
                };

                UserService.UpdateUser(dbUser);

                if (userCurrent.Id == user.Id && !user.IsAdmin) return RedirectToAction("LogOff", "Account");

                return RedirectToAction("Index");
            }

            return View(user);
        }

        [AdminAuthorize]
        public ActionResult Delete(int id)
        {
            UserService.DeleteUser(id);
            return RedirectToAction("Index");
        }

        private List<UserVM> FilterUsers(List<UserVM> model, string option, string search)
        {
            switch (option)
            {
                case "isAdmin":
                    return model.Where(x => search == null || x.IsAdmin == true).ToList();
                case "isUser":
                    return model.Where(x => search == null || x.IsAdmin == false).ToList();
                case "Username":
                    return model.Where(x => search == null || x.UserName.ToLower().Contains(search.ToLower())).ToList();
                case "FLName":
                    return model.Where(x => search == null ||
                                        x.FirstName.ToLower().Contains(search.ToLower()) ||
                                        x.LastName.ToLower().Contains(search.ToLower())).ToList();
                case "All":
                    return model;
                default:
                    return model;
            }
        }
    }
}
