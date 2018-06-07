using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class DateTimeExtensions
    {
        private static readonly DayOfWeek FirstDayOfWeek = DayOfWeek.Monday;
        private static readonly DayOfWeek LastDayOfWeek = DayOfWeek.Sunday;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToTimestamp(this DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime ToDateTime(this long value)
        {
            return Epoch.AddSeconds(value);
        }

        public static string ToMySQLString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime GetFirstDayOfMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        public static DateTime GetFirstDayOfWeek(this DateTime dt)
        {
            if (dt.DayOfWeek == FirstDayOfWeek) return dt.Date;

            var tempDate = dt;
            while (tempDate.DayOfWeek != FirstDayOfWeek)
                tempDate = tempDate.AddDays(-1);
            return tempDate.Date;
        }

        public static DateTime GetLastDayOfWeek(this DateTime dt)
        {
            if (dt.DayOfWeek == LastDayOfWeek) return dt.Date;

            var tempDate = dt;
            while (tempDate.DayOfWeek != LastDayOfWeek)
                tempDate = tempDate.AddDays(1);
            return tempDate.Date;
        }

        public static string TimeAgo(this DateTime date)
        {
            TimeSpan timeSince = DateTime.Now.Subtract(date);
            if (timeSince.TotalMilliseconds < 1)
                return "not yet";
            if (timeSince.TotalMinutes < 1)
                return "just now";
            if (timeSince.TotalMinutes < 2)
                return "1 minute ago";
            if (timeSince.TotalMinutes < 60)
                return string.Format("{0} minutes ago", timeSince.Minutes);
            if (timeSince.TotalMinutes < 120)
                return "1 hour ago";
            if (timeSince.TotalHours < 24)
                return string.Format("{0} hours ago", timeSince.Hours);
            if (timeSince.TotalDays == 1)
                return "yesterday";
            if (timeSince.TotalDays < 7)
                return string.Format("{0} {1} ago", timeSince.Days, timeSince.Days == 1 ? "day" : "days");
            if (timeSince.TotalDays < 14)
                return "2 weeks ago";
            if (timeSince.TotalDays < 21)
                return "2 weeks ago";
            if (timeSince.TotalDays < 31)
                return "3 weeks ago";
            if (timeSince.TotalDays < 60)
                return "last month";
            if (timeSince.TotalDays < 365)
                return string.Format("{0} months ago", Math.Round(timeSince.TotalDays / 30));
            if (timeSince.TotalDays < 730)
                return "last year";

            if (date == DateTime.MinValue)
                return "unknown";

            //last but not least...
            return string.Format("{0} years ago", Math.Round(timeSince.TotalDays / 365));

        }
    }
}
