using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Core.Utilities;

namespace System
{
    public static class XmlReaderExtensions
    {
        public static bool IsXmlFeed(this XmlReader xmlReader)
        {
            try
            {
                SyndicationFeed.Load(xmlReader);
                return true;
            }
            catch
            {
                var rss10 = new Rss10FeedFormatter();
                return rss10.CanRead(xmlReader);
            }
        }
    }
}
