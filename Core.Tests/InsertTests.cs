using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Services;
using System.Xml;
using System.IO;
using Core.Data;
using System.Data;
using Core.Utilities;
using System.Net;
using Core.Models;

namespace Core.Tests
{
    [TestClass]
    public class InsertTests
    {
        [TestMethod]
        public void Metadata()
        {
            var article = new FeedService().GetArticle(5586);
            article.AddMetadata("test", false);
            article.AddMetadata("test", true);
            article.AddMetadata("test", false);
            article.SaveMetadata();

            Assert.IsTrue(!article.GetMetadataValue<bool>("test"));
        }

        [TestMethod]
        public void InsertOrder()
        {
            var pay = new PaymentService().GetPayment("123234");
            var pays = new PaymentService().GetPayments(13);
        }

        [TestMethod]
        public void AddUserGuids()
        {
            var dp = new DataProvider();
            var ds = dp.GetFromSelect("select * from User where Id = 13");
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var guid = dr["guid"].ToString();

                var html = string.Empty;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    html = wc.DownloadString("http://localhost:81/mail/articles?guid=" + guid);
                }
                Mail.SendMeAnEmail("rssheap - weekly newsletter " + DateTime.Now.ToString("D"), html);
            }
        }

        [TestMethod]
        public void InsertFeed()
        {
            List<string> texts = File.ReadAllLines("c://feeds.txt").ToList();

            foreach (string text in texts)
            {
                string url = text.Trim();
                if (string.IsNullOrEmpty(url)) continue;
                if (url == " ") continue;

                //new FeedService().ins(url);
            }
        }

        [TestMethod]
        public void InsertExisting()
        {
            //var url = "www.lingvisti.ba/blog/rss";
            //var id = new FeedService().InsertFeed(url);
        }
    }
}
