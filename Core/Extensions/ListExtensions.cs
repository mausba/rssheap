using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static string ToCommaSeparetedListOfIds(this List<int> ids)
        {
            return string.Join(",", ids);
        }

        public static List<int> RemoveDuplicates(this List<int> ids)
        {
            var result = new List<int>();
            foreach (var id in ids)
            {
                if (!result.Contains(id))
                    result.Add(id);
            }
            return result;
        }

        public static string ToCommaSeparetedListOfIdsWithoutCommas(this List<int> ids)
        {
            if (ids == null) return string.Empty;
            string result = string.Empty;
            for (int i = 0; i < ids.Count; i++)
            {
                if (i != 0)
                    result += ",";
                result += ids[i];
            }
            return result;
        }
    }
}
