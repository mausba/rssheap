using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Core.Caching
{
    public class CachePeriod
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public CachePeriod(int hours, int minutes, int seconds)
        {
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }

        public static CachePeriod ForHours(int hours) => new CachePeriod(hours, 0, 0);

        public static CachePeriod ForMinutes(int minutes) => new CachePeriod(0, minutes, 0);

        public static CachePeriod ForSeconds(int seconds) => new CachePeriod(0, 0, seconds);

        public DateTime ToExpirationDate() => DateTime.Now.AddHours(Hours)
                               .AddMinutes(Minutes)
                               .AddSeconds(Seconds);

        public string ToExpirationDateString() => ToExpirationDate().ToString("(dd,MM,yyyy,HH,mm,ss)", CultureInfo.InvariantCulture);

        internal string GetKey() => Hours + "H," + Minutes + "M," + Seconds + "S";
    }
}
