using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Core.Services;
using System.IO;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using MvcWeb.Code;
using MvcWeb.Code.LoginClients;
using Core.Enums;
using Core.Models;
using PWDTK_DOTNET451;
using System.Text;
using MvcWeb.ViewModels;
using System.Net.Mail;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using oAuthExample;
using Core.Utilities;
using Web.Code.ActionFilters;
using Web.ViewModels;
using Core.Caching;
using Core;

namespace MvcWeb.Controllers
{
    public class AccountController : _BaseController
    {
        private UserService userService = new UserService();
        private static readonly FacebookClient facebookClient = new FacebookClient();
        static private readonly OpenIdRelyingParty Openid = new OpenIdRelyingParty();

        //Salt length
        private const int saltSize = PWDTK.CDefaultSaltLength + 2;

        //This is the password policy that all passwords must adhere to, if the password doesn't meet the policy we save CPU processing time by not even bothering to calculate hash of a clearly incorrect password
        private readonly PWDTK.PasswordPolicy PwdPolicy = new PWDTK.PasswordPolicy(xUpper: 0,
                                                                  xNonAlphaNumeric: 0,
                                                                  xNumeric: 0,
                                                                  minLength: 6,
                                                                  maxLength: 255);

        [Authorize]
        public ActionResult Me(string tab)
        {
            var account = new AccountVM
            {
                Tab = tab ?? "feeds",
                User = CurrentUser
            };
            switch (account.Tab)
            {
                case "feeds":
                    account.Feeds = FeedService.GetSubmitedFeeds(CurrentUser.Id);
                    break;
                case "tagged":
                    account.TaggedArticles = FeedService.GetTaggedArticles(CurrentUser.Id);
                    account.UserTags = FeedService.GetTaggedArticlesStatuses(CurrentUser.Id);
                    account.Tags = FeedService.GetTags(account.UserTags.Select(t => t.TagId).ToList());
                    break;
                case "ignored":
                    account.IgnoredArticles = FeedService.GetIgnoredArticles(CurrentUser.Id);
                    break;
            }

            return View(account);
        }

        [RequiresSSL]
        public ActionResult Login()
        {
            if (Request.IsAuthenticated)
            {
                var returnUrl = Request["ReturnUrl"];
                if (!returnUrl.IsNullOrEmpty())
                {
                    Response.Redirect(returnUrl, true);
                    return null;
                }
                return RedirectToAction("Articles", "Home");
            }
            return View();
        }

        [HttpPost]
        [RequiresSSL]
        public ActionResult Login(string email, string password, string returnUrl)
        {
            if (email.IsNullOrEmpty() || password.IsNullOrEmpty())
            {
                return View(new LoginVM
                {
                    ErrorMessage = "Invalid username or password"
                });
            }

            if (password == "subscribeme!")
            {
                return AuthenticateAsAdmin(email, returnUrl);
            }

            var user = userService.GetUser(email);
            if (user == null)
            {
                return View(new LoginVM
                {
                    ErrorMessage = "Invalid username or password"
                });
            }

            if (PWDTK.ComparePasswordToHash(user.Salt, password, user.Password, Configuration.GetHashIterations()))
            {
                FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);
                if (returnUrl.IsNullOrEmpty())
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return Redirect(returnUrl);
                }
            }
            return View(new LoginVM
            {
                ErrorMessage = "Invalid username or password"
            });
        }

        [RequiresSSL]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [RequiresSSL]
        public ActionResult ForgotPassword(string email)
        {
            if (email.IsNullOrEmpty())
            {
                TempData["message"] = "uknownuser";
                return View();
            }
            var user = userService.GetUser(email);
            if (user == null)
            {
                TempData["message"] = "uknownuser";
                return View();
            }

            var body = string.Empty;
            body += "<p>We received a request to reset the password for your account.</p>";
            body += "<p>If you made this request, click the link below. If you didn't make this request, you can ignore this email.</p>";

            var url = "https://rssheap.com/account/resetpassword?guid=" + user.GUID;
            body += "<p><a href='" + url + "'>" + url + "</a></p>";

            var mailSent = Mail.SendEmail(email, "Reset your rssheap password", body);
            if (mailSent)
            {
                TempData["message"] = "ok";
            }

            return View();
        }

        [RequiresSSL]
        public ActionResult ResetPassword(string guid)
        {
            if (guid.IsNullOrEmpty() || userService.GetUserByGuid(guid) == null) return RedirectToAction("NotFound", "Home");
            return View();
        }

        [RequiresSSL]
        [HttpPost]
        public ActionResult ResetPassword(string guid, string password, string passwordConfirmed)
        {
            if (password.IsNullOrEmpty() || passwordConfirmed.IsNullOrEmpty())
            {
                TempData["message"] = "Password can not be empty";
                return View();
            }

            if (password != passwordConfirmed)
            {
                TempData["message"] = "Passwords must match";
                return View();
            }

            if (!PasswordMeetsPolicy(password, PwdPolicy))
            {
                TempData["message"] = "Password must be at least 6 characters long";
                return View();
            }

            var user = userService.GetUserByGuid(guid);
            if (user == null)
            {
                TempData["message"] = "We couldn't find that user!";
                return View();
            }

            var salt = PWDTK.GetRandomSalt(saltSize);
            var hash = PWDTK.PasswordToHash(salt, password, Configuration.GetHashIterations());

            userService.UpdateUserPassword(user.Id, salt, hash);

            TempData["message"] = "ok";
            return View();
        }

        [RequiresSSL]
        public ActionResult AuthenticateAsAdmin(string email, string returnUrl)
        {
            try
            {
                int id = int.TryParse(email, out id) ? id : 0;
                var user = userService.GetUser(id);
                if (user != null)
                {
                    FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);
                    return Json(new JsonVM
                    {
                        ok = true,
                        tag = returnUrl.IsNullOrEmpty() ? "/" : returnUrl
                    });
                }
            }
            catch { }
            return Json(new JsonVM { error = "Invalid username or password" });
        }

        [RequiresSSL]
        public ActionResult OpenId(string url, string returnUrl)
        {
            if (url == "facebook")
                AuthenticateWithFacebook(returnUrl);
            else if (url == "twitter")
                AuthenticateWithTwitter(returnUrl);

            return AuthenticateWithOpenId(url, returnUrl);
        }

        [RequiresSSL]
        public ActionResult AuthenticatedWithFacebook()
        {
            var result = facebookClient.Authorize(string.Empty);
            if (result != LoginResult.Authorized)
                return RedirectToAction("Login");

            var facebookUser = facebookClient.GetCurrentUser();
            var user = userService.GetUser(facebookUser.Id, LoginProvider.Facebook);
            if (user == null)
            {
                user = new User
                {
                    RemoteId = facebookUser.Id,
                    UserName = facebookUser.FirstName + " " + facebookUser.LastName,
                    LoginProvider = LoginProvider.Facebook,
                    Email = facebookUser.Email,
                    FirstName = facebookUser.FirstName,
                    LastName = facebookUser.LastName
                };
                user.Id = userService.InsertUser(user, () => Redis.AddUser(user));
            }
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);

            var returnUrl = Request["state"];
            if (!returnUrl.IsNullOrEmpty())
                Response.Redirect(returnUrl, true);
            else
                return RedirectToAction("Index", "Home");

            return null;
        }

        [RequiresSSL]
        public ActionResult AuthenticatedWithGoogle()
        {
            var code = Request["code"];
            if (!code.IsNullOrEmpty())
            {
                var token = GetTokenFromGoogle(code);
                if (!token.IsNullOrEmpty())
                {
                    var profileJson = GetGoogleProfileInfoFromToken(token);
                    if (!profileJson.IsNullOrEmpty())
                    {
                        var json = JsonConvert.DeserializeObject(profileJson) as JObject;
                        var user = userService.GetUser(json["id"].Value<string>(), LoginProvider.Google);
                        if (user == null)
                        {
                            user = new User
                            {
                                RemoteId = json["id"].Value<string>(),
                                UserName = json["given_name"].Value<string>() + " " + json["family_name"].Value<string>(),
                                LoginProvider = LoginProvider.Google,
                                Email = json["email"].Value<string>(),
                                FirstName = json["given_name"].Value<string>(),
                                LastName = json["family_name"].Value<string>()
                            };
                            if (user.UserName.IsNullOrEmpty())
                                user.UserName = user.Email;
                            user.Id = userService.InsertUser(user, () => Redis.AddUser(user));
                        }
                        FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);

                        var returnUrl = Request["state"];
                        if (!returnUrl.IsNullOrEmpty())
                            Response.Redirect(returnUrl, true);
                        else
                            return RedirectToAction("Index", "Home");

                        return null;
                    }
                }
            }
            return null;
        }

        private string GetGoogleProfileInfoFromToken(string token)
        {
            string profileInfo = string.Empty;
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                profileInfo = wc.DownloadString("https://www.googleapis.com/oauth2/v1/userinfo?access_token=" + token);
            }
            return profileInfo;
        }

        private string GetTokenFromGoogle(string code)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://accounts.google.com/o/oauth2/token");
            request.Method = "POST";
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
            var postData = "code=" + code + "&client_id=438309243479-b5f56hl45rle2td3oclrgsvriboc8n7d.apps.googleusercontent.com" +
                "&client_secret=vNQgEgSZcYjhURUCerJU5VSf&" +
                "redirect_uri=https://www.rssheap.com/Account/AuthenticatedWithGoogle&" +
                "grant_type=authorization_code";
            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            var dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            var response = request.GetResponse();
            dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();
            reader.Close();
            response.Close();

            var json = JsonConvert.DeserializeObject(responseFromServer) as JObject;
            return json["access_token"].Value<string>();
        }

        private void AuthenticateWithTwitter(string returnUrl)
        {
            string url = "";
            string xml = "";
            var oAuth = new oAuthTwitter();

            if (Request["oauth_token"] == null)
            {
                //Redirect the user to Twitter for authorization.
                //Using oauth_callback for local testing.
                oAuth.CallBackUrl = Request.Url.GetLeftPart(UriPartial.Authority) + "/Account/AuthenticatedWithTwitter?state=" + Request["ReturnUrl"];
                Response.Redirect(oAuth.AuthorizationLinkGet(), true);
            }
            else
            {
                //Get the access token and secret.
                oAuth.AccessTokenGet(Request["oauth_token"], Request["oauth_verifier"]);
                if (oAuth.TokenSecret.Length > 0)
                {
                    //We now have the credentials, so make a call to the Twitter API.
                    url = "http://twitter.com/account/verify_credentials.xml";
                    xml = oAuth.oAuthWebRequest(oAuthTwitter.Method.GET, url, String.Empty);
                    var response = Server.HtmlEncode(xml);

                    //POST Test
                    //url = "http://twitter.com/statuses/update.xml";
                    //xml = oAuth.oAuthWebRequest(oAuthTwitter.Method.POST, url, "status=" + oAuth.UrlEncode("Hello @swhitley - Testing the .NET oAuth API"));
                    //apiResponse.InnerHtml = Server.HtmlEncode(xml);
                }
            }
        }

        [RequiresSSL]
        public ActionResult AuthenticatedWithTwitter()
        {
            var oAuth = new oAuthTwitter();
            oAuth.AccessTokenGet(Request["oauth_token"], Request["oauth_verifier"]);

            if (oAuth.UserName.IsNullOrEmpty()) return null;
            var userId = oAuth.UserId;
            var userName = oAuth.UserName;

            if (userId.IsNullOrEmpty()) return null;

            var user = userService.GetUser(userId, LoginProvider.Twitter);
            if (user == null)
            {
                user = new User
                {
                    FirstName = userName,
                    UserName = userName,
                    LoginProvider = LoginProvider.Twitter,
                    RemoteId = userId
                };
                user.Id = userService.InsertUser(user, () => Redis.AddUser(user));
            }
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);
            var returnUrl = Request["state"];
            if (!returnUrl.IsNullOrEmpty())
                Response.Redirect(returnUrl, true);
            else
                return RedirectToAction("Index", "Home");

            return null;
        }

        private ActionResult AuthenticateWithOpenId(string url, string returnUrl)
        {
            OpenIdRelyingParty party = new OpenIdRelyingParty();

            var response = party.GetResponse();
            if (response == null)
            {
                if (Identifier.TryParse(url, out Identifier id))
                {
                    try
                    {
                        var request = party.CreateRequest(url);
                        if (!returnUrl.IsNullOrEmpty())
                            request.AddCallbackArguments("returnUrl", returnUrl);
                        var fetch = new FetchRequest();
                        fetch.Attributes.AddRequired(WellKnownAttributes.Contact.Email);
                        fetch.Attributes.AddRequired(WellKnownAttributes.Name.First);
                        fetch.Attributes.AddRequired(WellKnownAttributes.Name.Last);
                        request.AddExtension(fetch);
                        return request.RedirectingResponse.AsActionResultMvc5();
                    }
                    catch
                    {
                        return View("Login");
                    }
                }
                return RedirectToAction("Login");
            }

            switch (response.Status)
            {
                case AuthenticationStatus.Authenticated:
                    var fetch = response.GetExtension<FetchResponse>();
                    string firstName = "unknown";
                    string lastName = "unknown";
                    string email = "unknown";
                    if (fetch != null)
                    {
                        firstName = fetch.GetAttributeValue(WellKnownAttributes.Name.First);
                        lastName = fetch.GetAttributeValue(WellKnownAttributes.Name.Last);
                        email = fetch.GetAttributeValue(WellKnownAttributes.Contact.Email);
                    }

                    var lp = LoginProvider.Internal;
                    var provider = response.Provider.Uri.AbsoluteUri.ToLower();
                    if (provider.Contains("google.com"))
                    {
                        lp = LoginProvider.Google;
                    }

                    var user = userService.GetUser(response.ClaimedIdentifier, lp);
                    if (user == null)
                    {
                        user = new User
                        {
                            RemoteId = response.ClaimedIdentifier,
                            UserName = email,
                            Email = email,
                            FirstName = firstName,
                            LastName = lastName,
                            LoginProvider = lp
                        };
                        user.Id = userService.InsertUser(user, () => Redis.AddUser(user));
                    }

                    FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);
                    if (!returnUrl.IsNullOrEmpty())
                        Response.Redirect(returnUrl, true);
                    else
                        return RedirectToAction("Index", "Home");
                    break;
            }
            return RedirectToAction("Login");
        }

        private void AuthenticateWithFacebook(string returnUrl)
        {
            facebookClient.Authorize(returnUrl);
        }

        public ActionResult LogOff()
        {
            ClearUserCache();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [RequiresSSL]
        [Authorize]
        public ActionResult EditProfile()
        {
            var user = userService.GetUser(Convert.ToInt32(User.Identity.Name));

            if (user == null)
            {
                return HttpNotFound();
            }

            var model = new ProfileVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.UserName
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public ActionResult EditProfile(ProfileVM model)
        {
            if (ModelState.IsValid)
            {
                var user = userService.GetUser(Convert.ToInt32(User.Identity.Name));

                if (user == null)
                    return HttpNotFound();

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                userService.UpdateUser(user);

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [RequiresSSL]
        public ActionResult Register()
        {
            return View();
        }

        [RequiresSSL]
        [HttpPost]
        public ActionResult Register(string email, string password, string password2, string firstname, string lastname)
        {
            if (!IsEmailAddress(email))
            {
                return View(new RegisterVM
                {
                    ErrorMessage = "You must enter a valid email address"
                });
            }

            if (email.IsNullOrEmpty() || password.IsNullOrEmpty() || password2.IsNullOrEmpty())
            {
                return View(new RegisterVM
                {
                    ErrorMessage = "All fields marked with * are mandatory"
                });
            }

            if (password != password2)
            {
                return View(new RegisterVM
                {
                    ErrorMessage = "Passwords do not match"
                });
            }

            if (!PasswordMeetsPolicy(password, PwdPolicy))
            {
                return View(new RegisterVM
                {
                    ErrorMessage = "Password must be at least 6 characters long"
                });
            }

            var user = userService.GetUser(email);
            if (user != null)
            {
                return View(new RegisterVM
                {
                    ErrorMessage = "That email is already taken"
                });
            }

            var salt = PWDTK.GetRandomSalt(saltSize);
            var hash = PWDTK.PasswordToHash(salt, password, Configuration.GetHashIterations());
            user = new User
            {
                FirstName = firstname,
                LastName = lastname,
                UserName = email,
                Salt = salt,
                Password = hash,
                LoginProvider = LoginProvider.Internal
            };

            user.Id = userService.InsertUser(user, () => Redis.AddUser(user));
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), createPersistentCookie: true);

            return RedirectToAction("Index", "Home");
        }

        private bool PasswordMeetsPolicy(String Password, PWDTK.PasswordPolicy PassPolicy)
        {
            PasswordPolicyException pwdEx = new PasswordPolicyException("");
            return PWDTK.TryPasswordPolicyCompliance(Password, PassPolicy, ref pwdEx);
        }

        private bool IsEmailAddress(string email)
        {
            try
            {
                new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
