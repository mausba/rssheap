using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using Core.Enums;
using HtmlAgilityPack;
using System.IO;
using System.Globalization;
using Core.Utilities;
using System.Net.Mail;

namespace System
{
    public static class StringExtensions
    {
        public static string ToBase64(this string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static int ToInt(this string str)
        {
            if (str.IsNullOrEmpty()) return 0;
            int intValue = int.TryParse(str, out intValue) ? intValue : 0;
            return intValue;
        }

        public static bool ToBool(this string str)
        {
            if (str.IsNullOrEmpty()) return false;
            bool boolValue = bool.TryParse(str, out boolValue) ? boolValue : false;
            return boolValue;
        }

        public static string FromBase64(this string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        public static bool IsEmailAddress(this string email)
        {
            try
            {
                new MailAddress(email);
                return true;
            }
            catch
            { }
            return false;
        }

        public static string ParseRelativeUrls(this string html, string url)
        {
            try
            {
                if (string.IsNullOrEmpty(html)) return html;

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var baseUrl = new Uri(url).GetLeftPart(UriPartial.Authority);

                var imgNodes = htmlDocument.DocumentNode.SelectNodes("//img");
                if(imgNodes != null)
                {
                    foreach (var img in imgNodes)
                    {
                        var imgSrcAttr = img.Attributes["src"];
                        if (imgSrcAttr == null) continue;

                        var imgSrc = imgSrcAttr.Value;
                        if (imgSrc.IsNullOrEmpty()) continue;

                        if (imgSrc.StartsWith("http") || imgSrc.StartsWith("www")) continue;
                        img.Attributes["src"].Value = new Uri(new Uri(baseUrl), imgSrc).AbsoluteUri;
                    }
                }

                var aNodes = htmlDocument.DocumentNode.SelectNodes("//a");
                if (aNodes != null)
                {
                    foreach (var a in aNodes)
                    {
                        var aHrefAttr = a.Attributes["href"];
                        if (aHrefAttr == null) continue;

                        var aHref = aHrefAttr.Value;
                        if (aHref.IsNullOrEmpty()) continue;

                        if (aHref.StartsWith("http") || aHref.StartsWith("www")) continue;
                        a.Attributes["href"].Value = new Uri(new Uri(baseUrl), a.Attributes["href"].Value).AbsoluteUri;
                    }
                }

                if (aNodes == null && imgNodes == null)
                    return html;

                var ws = new StringWriter();
                htmlDocument.Save(ws);

                return ws.ToString();
            }
            catch (Exception ex)
            {
                Mail.SendMeAnEmail("Error in ParseRelativeUrls", ex.ToString());
            }
            return html;
        }

        public static bool IsDateTimeMy(this string str)
        {
            if (str.IsNullOrEmpty()) return false;
            try
            {
                DateTime.ParseExact(str, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                return true;
            }
            catch { }
            return false;
        }

        public static bool IsNumber(this string str)
        {
            if (str.IsNullOrEmpty()) return false;
            foreach (char c in str)
                if (!char.IsNumber(c)) return false;
            return true;
        }

        public static string ToTagName(this string str)
        {
            if (str.IsNullOrEmpty()) return null;
            str = str.Trim();

            var regexMultipleSpaces = new Regex(@"[ ]{2,}", RegexOptions.None);
            str = regexMultipleSpaces.Replace(str, @" ");
           
            var regexSpacesWithDash = new Regex(@"[ ]{1,}", RegexOptions.None);
            return regexSpacesWithDash.Replace(str, @"-");
        }

        public static List<string> SplitWithoutEmptyEntries(this string str, string separator = ",")
        {
            if (str.IsNullOrEmpty()) return new List<string>();
            return str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static Dictionary<string, int> GetWordOccurences(this string text)
        {
            var result = new Dictionary<string, int>();
            var words = text.CustomSplit(' ');
            foreach (var word in words)
            {
                int currentCount = 0;
                result.TryGetValue(word, out currentCount);

                currentCount++;
                result[word] = currentCount;
            }
            return result;
        }

        public static IEnumerable<string> CustomSplit(this string newtext, char splitChar)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inside = false;
            foreach (var c in newtext)
            {
                if (c == '<')
                {
                    inside = true;
                    continue;
                }
                if (c == '>')
                {
                    inside = false;
                    continue;
                }
                if (c == '\r' || c == '\n') continue;
                if (inside) continue;

                if ((c == splitChar))
                {
                    if (sb.Length > 0)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    continue;
                }
                sb.Append(c);
            }
            if (sb.Length > 0)
            {
                result.Add(sb.ToString());
            }
            return result;
        }

        public static string GenerateSlug(this string phrase)
        {
            if (phrase.IsNullOrEmpty()) return string.Empty;
            string str = phrase.RemoveAccent().ToLower();
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 79 ? str.Length : 79).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }

        public static string RemoveAccent(this string txt)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static List<int> ToCommaSeparatedListOfIds(this string ids)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(ids)) return result;
            foreach (var id in ids.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    result.Add(int.Parse(id));
            }
            return result;
        }

        public static Dictionary<int, string> ToCommaSeparatedDictionaryIntString(this string ids)
        {
            var result = new Dictionary<int, string>();
            if (string.IsNullOrEmpty(ids)) return result;
            foreach (var keyValue in ids.Split(','))
            {
                if (string.IsNullOrEmpty(keyValue)) continue;
                string key = keyValue.Split(':')[0];
                string value = keyValue.Split(':')[1];

                result.Add(int.Parse(key), value);
            }
            return result;
        }

        public static Dictionary<string, int> ToCommaSeparatedDictionaryStringInt(this string ids)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(ids)) return result;
            foreach (var keyValue in ids.Split(','))
            {
                if (string.IsNullOrEmpty(keyValue)) continue;
                string key = keyValue.Split(':')[0];
                int value = int.Parse(keyValue.Split(':')[1]);

                result.Add(key, value);
            }
            return result;
        }

        public static Dictionary<int, int> ToCommaSeparatedDictionaryIntInt(this string ids)
        {
            var result = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(ids)) return result;
            foreach (var keyValue in ids.Split(','))
            {
                if (string.IsNullOrEmpty(keyValue)) continue;
                int key = int.Parse(keyValue.Split(':')[0]);
                int value = int.Parse(keyValue.Split(':')[1]);

                result.Add(key, value);
            }
            return result;
        }

        public static string EncodeForUrl(this string text)
        {
            text = text.Replace('?', ' ');
            string encoded = text.ToLower();
            encoded = encoded.Replace('-', ' ');
            encoded = encoded.Replace(' ', '-');
            encoded = encoded.Replace("č", "c");
            encoded = encoded.Replace("ž", "z");
            encoded = encoded.Replace("š", "s");
            return encoded;
        }

        public static string StripHtml(this string text)
        {
            char[] array = new char[text.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < text.Length; i++)
            {
                char let = text[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            string newString = new string(array, 0, arrayIndex);

            if (newString.StartsWith(" "))
                newString = newString.Substring(1, newString.Length - 1);

            newString = newString.Replace("\n", " ").Replace("\r\n", string.Empty);
            newString = newString.Replace("\t", " ").Replace("\r\t", string.Empty);
            newString = newString.Replace("&nbsp;", string.Empty);

            //remove multiple spaces with single space
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex(@"[ ]{2,}", options);
            newString = regex.Replace(newString, @" ");

            return newString;
        }

        public static string Shorten(this string text, int count)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= count) return text;
            return text.Substring(0, count - 3) + "...";
        }
    }
}
