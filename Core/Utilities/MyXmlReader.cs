using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Core.Utilities
{
    public class MyXmlReader : XmlTextReader
    {
        private bool readingDate = false;
        const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z yyyy"; // Wed Oct 07 08:00:07 GMT 2009
        private readonly XmlReaderSettings XmlReaderSettings;

        public MyXmlReader(TextReader input, XmlReaderSettings settings)
            : base(input)
        {
            XmlReaderSettings = settings;
        }

        public override XmlReaderSettings Settings
        {
            get
            {
                return XmlReaderSettings;
            }
        }

        public override void ReadStartElement()
        {
            if (string.Equals(base.NamespaceURI, string.Empty, StringComparison.InvariantCultureIgnoreCase) &&
                (string.Equals(base.LocalName, "lastBuildDate", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(base.LocalName, "pubDate", StringComparison.InvariantCultureIgnoreCase)))
            {
                readingDate = true;
            }
            base.ReadStartElement();
        }

        public override void ReadEndElement()
        {
            if (readingDate)
            {
                readingDate = false;
            }
            base.ReadEndElement();
        }

        public override string ReadString()
        {
            if (readingDate)
            {
                string dateString = base.ReadString();
                if (!DateTime.TryParse(dateString, out DateTime dt))
                {
                    try
                    {
                        dt = DateTime.ParseExact(dateString, CustomUtcDateTimeFormat, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        var ci = CultureInfo.GetCultureInfo("en-US");
                        var formats = ci.DateTimeFormat.GetAllDateTimePatterns();
                        if (!DateTime.TryParseExact(dateString, formats, ci, DateTimeStyles.AssumeUniversal, out dt))
                        {
                            try
                            {
                                if (dateString.IndexOf("GMT") > 0)
                                {
                                    dateString = dateString.Substring(0, dateString.IndexOf("GMT")).Trim();
                                    dt = DateTime.Parse(dateString);
                                }
                            }
                            catch { }
                        }
                    }
                }
                var date = dt.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
                return date;
            }
            else
            {
                return base.ReadString();
            }
        }
    }
}
