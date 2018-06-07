using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());
            MvcHandler.DisableMvcResponseHeader = true;
        }

        void Application_Error(object sender, EventArgs e)
        {
            var url = HttpContext.Current.Request.Url.AbsoluteUri;

            var ex = Server.GetLastError();
            if (ex == null) return;

            string errorMessage = string.Empty;
            errorMessage += "<br/><br/> Referer: " + Request.UrlReferrer + Environment.NewLine;

            while (ex != null)
            {
                errorMessage += ex.ToString();
                ex = ex.InnerException;
            }

            errorMessage += "Server variables:" + Environment.NewLine + "<br/><br/>";
            foreach (var key in Request.ServerVariables.AllKeys)
            {
                errorMessage += key + " : " + Request.ServerVariables[key] + Environment.NewLine + "<br/><br/>";
            }
			
			errorMessage += "Form variables:" + Environment.NewLine + "<br/><br/>";
			foreach (var key in Request.Form.AllKeys)
            {
                errorMessage += key + " : " + Request.Form[key] + Environment.NewLine + "<br/><br/>";
            }

            if (!url.Contains("localhost"))
                SendEmailEveryMinute("exception from nordsee", url + " <br/><br/> " + errorMessage);
        }

        public static DateTime EmailLastSent;
        private void SendEmailEveryMinute(string subject, string body)
        {
            if (DateTime.Now.AddMinutes(-1) > EmailLastSent)
            {
                EmailLastSent = DateTime.Now;
                Mail.SendMeAnEmail(subject, body);
            }
        }
    }
}
