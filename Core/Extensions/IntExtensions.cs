using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class IntExtensions
    {
        public static string FormatCount(this int count)
        {
            if (count >= 1000000)
            {
                var millions = count / 1000000;
                var thousands = (count - (millions * 1000000)) / 1000;
                if (thousands == 0)
                {
                    return millions + "M";
                }
                return millions + "," + thousands.ToString()[0] + "M";
            }

            if (count >= 1000)
                return count / 1000 + "k";

            return count.ToString();
        }
    }
}
