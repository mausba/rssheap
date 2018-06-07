using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.Utilities;
using MvcWeb.Controllers;
using Web.ViewModels;
using System.Diagnostics;

namespace Web.Controllers
{
    [Authorize]
    public class PaypalController : _BaseController
    {
        private readonly bool sandbox;
        private decimal amount;

        public PaypalController()
        {
            sandbox = Debugger.IsAttached;
            amount = 9.99m;
        }

        public void Order()
        {
            if (CurrentUser.SharedOnTwitter) amount -= 5;
            if (CurrentUser.SharedOnFacebook) amount -= 5;

            var business = sandbox ? "dzlotrg-facilitator@hotmail.com" : "dzlotrg@gmail.com";

            var domain = Request.Url.AbsoluteUri.Contains("localhost")
                ? "http://localhost:81" :
                "https://www.rssheap.com";

            var paypalUrl = sandbox
                ? "https://www.sandbox.paypal.com/cgi-bin/webscr?"
                : "https://www.paypal.com/cgi-bin/webscr?";

            var sb = new StringBuilder();
            sb.Append("cmd=_xclick");
            sb.Append("&business=" + HttpUtility.UrlEncode(business));
            sb.Append("&no_shipping=1");
            sb.Append("&item_name=" + HttpUtility.UrlEncode("rssheap pro (1 year subscription)"));
            sb.Append("&currency_code=USD");
            sb.Append("&amount=" + HttpUtility.UrlEncode(amount.ToString().Replace(",", ".")));
            sb.Append("&RETURNURL=" + domain + "/paypal/IPN?userid=" + CurrentUser.Id);
            sb.Append("&return=" + domain + "/paypal/IPN?userid=" + CurrentUser.Id);
            sb.Append("&notify_url=" + domain + "/paypal/IPN?userid=" + CurrentUser.Id);

            Response.Redirect(paypalUrl + sb.ToString());
        }

        public ActionResult IPN()
        {
            try
            {
                var formVals = new Dictionary<string, string>
                {
                    { "cmd", "_notify-validate" }
                };

                var user = UserService.GetUser(int.Parse(Request["userid"]));
                if (user != null)
                {
                    string formValues = string.Empty;
                    foreach (string form in Request.Form.Keys)
                    {
                        formValues += form + " : " + Request[form] + Environment.NewLine;
                    }
                    var payment = new Payment
                    {
                        Amount = amount,
                        Date = DateTime.Now,
                        Email = user.Email,
                        OrderType = "year",
                        TransactionId = "-1",
                        UserId = user.Id,
                        IsNew = true,
                        FormValues = formValues
                    };

                    PaymentService.InsertPayment(payment);
                    PaymentService.ClearCachedInfo(user);

                    ActionExtensions.TryAction(() =>
                    {
                        Mail.SendMeAnEmail("New payment on the site", "woohoo");
                    });

                    return View(new PaymentVM
                    {
                        User = user,
                        Payment = payment
                    });
                }


                string response = GetPayPalResponse(formVals);

                if (response.ToUpper().Contains("VERIFIED"))
                {
                    var transactionID = Request["txn_id"];
                    var sAmountPaid = Request["mc_gross"];
                    var payerEmail = Request["payer_email"];
                    var Item = Request["item_name"];

                    //validate the order
                    Decimal.TryParse(sAmountPaid, out decimal amountPaid);
                    if (amountPaid >= amount)
                    {
                        string formValues = string.Empty;
                        foreach (string form in Request.Form.Keys)
                        {
                            formValues += form + " : " + Request[form] + Environment.NewLine;
                        }

                        var payment = PaymentService.GetPayment(transactionID);
                        if (payment == null)
                        {
                            payment = new Payment
                            {
                                Amount = amount,
                                Date = DateTime.Now,
                                Email = payerEmail,
                                OrderType = "year",
                                TransactionId = transactionID,
                                UserId = user.Id,
                                IsNew = true,
                                FormValues = formValues
                            };

                            PaymentService.InsertPayment(payment);
                            PaymentService.ClearCachedInfo(user);

                            ActionExtensions.TryAction(() =>
                            {
                                Mail.SendMeAnEmail("New payment on the site", "woohoo");
                            });
                        }

                        return View(new PaymentVM
                        {
                            User = user,
                            Payment = payment
                        });
                    }
                    else
                    {
                        Mail.SendMeAnEmail("User did not pay the right amount",
                            "trans:" + transactionID + " amount: " + amount + " email: " + payerEmail + " userid: " + user.Id);
                    }
                }
                else
                {
                    Mail.SendMeAnEmail("IPN fail", "response: " + response + " transid:" + Request["txn_id"] + " userid: " + user.Id);
                }
            }
            catch (Exception ex)
            {
                Mail.SendMeAnEmail("Error in IPN", ex.ToString());
            }
            return RedirectToAction("NotFound", "Home");
        }

        string GetPayPalResponse(Dictionary<string, string> formVals)
        {
            try
            {
                string paypalUrl = sandbox
                    ? "https://www.sandbox.paypal.com/cgi-bin/webscr"
                    : "https://www.paypal.com/cgi-bin/webscr";

                var req = (HttpWebRequest)WebRequest.Create(paypalUrl);

                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";

                byte[] param = Request.BinaryRead(Request.ContentLength);
                string strRequest = Encoding.ASCII.GetString(param);
                if (strRequest.IsNullOrEmpty()) return string.Empty;

                var sb = new StringBuilder();
                sb.Append(strRequest);

                foreach (string key in formVals.Keys)
                {
                    sb.AppendFormat("&{0}={1}", key, formVals[key]);
                }
                strRequest += sb.ToString();

                req.ContentLength = strRequest.Length;

                var response = string.Empty;
                var streamOut = new StreamWriter(req.GetRequestStream(), Encoding.ASCII);
                streamOut.Write(strRequest);
                streamOut.Close();
                var streamIn = new StreamReader(req.GetResponse().GetResponseStream());
                response = streamIn.ReadToEnd();
                return response;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}