using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static string ToCommaSeparatedList(this Dictionary<int, string> idCounts)
        {
            string result = string.Empty;
            if (idCounts == null) return result;
            result += ",";
            var keys = idCounts.Keys.OrderBy(k => k).ToList();
            foreach (var key in keys)
            {
                result += key + ":" + idCounts[key] + ",";
            }
            return result;
        }

        public static string ToCommaSeparatedList(this Dictionary<int, int> idCounts)
        {
            string result = string.Empty;
            if (idCounts == null) return result;
            result += ",";
            var keys = idCounts.Keys.OrderBy(k => k).ToList();
            foreach (var key in keys)
            {
                result += key + ":" + idCounts[key] + ",";
            }
            return result;
        }

        public static string ToCommaSeparatedList(this Dictionary<string, string> idCounts)
        {
            string result = string.Empty;
            if (idCounts == null) return result;
            result += ",";
            var keys = idCounts.Keys.OrderBy(k => k).ToList();
            foreach (var key in keys)
            {
                result += key + ":" + idCounts[key] + ",";
            }
            return result;
        }

        public static string ToCommaSeparatedList(this Dictionary<string, int> idCounts)
        {
            string result = string.Empty;
            if (idCounts == null) return result;
            result += ",";
            var keys = idCounts.Keys.OrderBy(k => k).ToList();
            foreach (var key in keys)
            {
                result += key + ":" + idCounts[key] + ",";
            }
            return result;
        }
    }
}
