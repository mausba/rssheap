using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace CDN.Controllers
{
    public class ImagesController : Controller
    {
        public ActionResult favicon(string domain)
        {
            var fileName = Server.MapPath("/favicons/" + HttpUtility.UrlEncode(domain) + ".png");

            if (!System.IO.File.Exists(fileName))
            {
                var faviconUrl = "http://www.google.com/s2/favicons?domain=" + domain;

                var client = new WebClient();
                client.DownloadFile(faviconUrl, fileName);
            }

            return File(fileName, "image/png");
        }
	}
}