using Core.Models;
using Core.Utilities;
using MvcWeb.Controllers;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Web.ViewModels;

namespace Web.Controllers
{
    [Authorize]
    public class CardController : _BaseController
    {
        private readonly string _stripeSecretKey;

        public CardController()
        {
            _stripeSecretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public ActionResult Index()
        {
            var user = CurrentUser;

            if (user != null && !PaymentService.IsPro(user))
            {
                if (TempData["StripeSessionId"] == null) return RedirectToAction("Index", "Articles", null);

                var payment = new Payment
                {
                    Amount = 1,
                    Date = DateTime.Now,
                    Email = user.Email,
                    OrderType = "year",
                    TransactionId = TempData["StripeSessionId"].ToString(),
                    UserId = user.Id,
                    IsNew = true,
                    FormValues = ""
                };

                PaymentService.InsertPayment(payment);
                PaymentService.ClearCachedInfo(user);

                Mail.SendMeAnEmail("New payment on the site", "woohoo");

                var model = new CardVM
                {
                    User = user,
                    Payment = payment,
                    isPro = false

                };

                return View(model);
            }
            else
            {
                var model = new CardVM
                {
                    User = user,
                    Payment = PaymentService.GetPayments(user.Id).Last(),
                    isPro = true
                };

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult Checkout()
        {
            try
            {
                var user = CurrentUser;

                if (user == null) return RedirectToAction("Login", "Account", null);

                if (!PaymentService.IsPro(user))
                {
                    var domain = $"{Request.Url.Scheme}://{HttpContext.Request.Url.Authority}";
                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>
                        {
                            new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    Currency = "usd",
                                    UnitAmount=long.Parse("999"),
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = "Pro Subscription for rssheap user",
                                        Description = "Yearly subscription to Pro features"
                                    },
                                },
                                Quantity = 1
                            },
                        },
                        Mode = "payment",
                        SuccessUrl = domain + "/card/index",
                        CancelUrl = domain + "/home/pro",
                        CustomerEmail = user.UserName,
                    };

                    var service = new SessionService();
                    var session = service.Create(options);

                    TempData["StripeSessionId"] = session.Id;

                    return Redirect(session.Url);
                }

                return RedirectToAction("NotFound", "Home");
            }
            catch (Exception ex)
            {
                Mail.SendMeAnEmail("Error in CardController - Checkout", ex.ToString());
                return RedirectToAction("NotFound", "Home");
            }
        }
    }
}
