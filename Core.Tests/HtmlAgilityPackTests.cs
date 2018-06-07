using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Core.Tests
{
    [TestClass]
    public class HtmlAgilityPackTests
    {
        private void CrashTest(Action action)
        {
            var aspNetStackSize = 256 * 1000;
            var thread = new Thread(() => action(), aspNetStackSize);
            thread.Start();
            thread.Join();
        }

        [TestMethod]
        public void Test_Unclosed_Nodes_Do_Not_Stackoverflow_Even_If_The_Dom_Is_Deep()
        {
            var spans = Enumerable.Repeat("<span>", 5000);
            var unclosed = String.Format("<div>{0}</div>", String.Join("", spans));
            CrashTest(() =>
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(unclosed);
            });
        }

        [TestMethod]
        public void Test_InnerOuter_Does_Not_Stackoverflow_Even_If_The_Dom_Is_Deep()
        {
            var deep = String.Join("",
                Enumerable.Repeat("<span>", 5000)
                    .Concat(Enumerable.Repeat("</span>", 5000))
            );

            CrashTest(() =>
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(deep);

                var innner = doc.DocumentNode.InnerText;
                var outerHtml = doc.DocumentNode.OuterHtml;
                var innerHtml = doc.DocumentNode.InnerHtml;
            });
        }

        [TestMethod]
        public void Test_Save_Does_Not_Stackoverflow_Even_If_The_Dom_Is_Deep()
        {
            var deep = String.Join("",
                Enumerable.Repeat("<div><span>", 1000)
                    .Concat(Enumerable.Repeat("</span></div>", 1000))
            );

            CrashTest(() =>
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                var writer = new StringWriter();

                doc.LoadHtml(deep);
                doc.Save(writer);
            });
        }
    }
}
