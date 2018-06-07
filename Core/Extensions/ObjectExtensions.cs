using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Newtonsoft.Json;

public static class ObjectExtensions
{
    private static Regex RegexRemoveTags = new Regex(@"<.*?>", RegexOptions.Compiled);
    private static Regex RegexRemoveWhitespaceChars = new Regex(@"\s+", RegexOptions.Compiled);

    public static string ToXml(this object obj)
    {
        if (obj == null) return "null";
        XmlSerializer serializer = new XmlSerializer(obj.GetType());
        TextWriter tw = new StringWriter();
        serializer.Serialize(tw, obj);
        return tw.ToString();
    }

    public static T FromXml<T>(this string xml)
    {
        if (xml.IsNullOrEmpty()) return default(T);
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        return (T) serializer.Deserialize(new StringReader(xml));
    }

    public static T Clone<T>(this T obj)
    {
        var xml = obj.ToXml();
        XmlSerializer serializer = new XmlSerializer(obj.GetType());
        return (T)serializer.Deserialize(new StringReader(xml));
    }

    public static string ToJson(this object obj)
    {
        if (obj == null) return null;
        return JsonConvert.SerializeObject(obj);
    }

    public static string ToCacheKey(this object obj)
    {
        if (obj == null) return null;

        var cacheKey = obj.ToJson();

        cacheKey = RegexRemoveWhitespaceChars.Replace(cacheKey, string.Empty);
        cacheKey = cacheKey.Trim();

        return obj.GetType().FullName + cacheKey;
    }
}