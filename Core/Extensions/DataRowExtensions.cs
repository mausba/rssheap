using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using Core.Enums;
using System.Globalization;

namespace System.Data
{
    public static class DataRowExtensions
    {
        public static string ToStringOrNull(this DataRow dr, string alias)
        {
            if (dr == null) return null;
            if (dr.Table.Columns[alias] == null) return null;

            if (dr == null) return null;
            if (dr[alias] != DBNull.Value) return dr[alias].ToString();

            return string.Empty;
        }

        public static byte[] ToBytesOrNull(this DataRow dr, string alias)
        {
            if (dr == null) return null;

            if (dr.Table.Columns[alias] == null) return null;

            if (dr == null) return null;
            if (dr[alias] != DBNull.Value) return (byte[])dr[alias];

            return null;
        }

        public static int ToIntOrZero(this DataRow dr, string alias)
        {
            if (dr == null) return 0;
            if (dr.Table.Columns[alias] == null) return 0;

            if (dr[alias] != DBNull.Value) return Convert.ToInt32(dr[alias]);

            return 0;
        }

        public static DateTime ToDateTimeOrMinValue(this DataRow dr, string alias)
        {
            if (dr == null) return DateTime.MinValue;
            if (dr.Table.Columns[alias] == null) return DateTime.MinValue;

            if (dr == null) return DateTime.MinValue;
            if (dr[alias] != DBNull.Value) return (DateTime) dr[alias];

            return DateTime.MinValue;
        }

        public static bool ToBoolOrFase(this DataRow dr, string alias)
        {
            if (dr == null) return false;
            if (dr.Table.Columns[alias] == null) return false;

            if (dr == null) return false;
            if (dr[alias] != DBNull.Value) return Convert.ToBoolean(dr[alias]);

            return false;
        }

        public static List<int> ToCommaSeparatedListOfIds(this DataRow dr, string alias)
        {
            if (dr == null) return new List<int>();
            if (dr.Table.Columns[alias] == null) return new List<int>();

            if (dr == null) return new List<int>();
            if (dr[alias] != DBNull.Value) return dr[alias].ToString().ToCommaSeparatedListOfIds();

            return new List<int>();
        }

        public static ArticleActions ToArticleAction(this DataRow dr, string alias)
        {
            if (dr == null) return ArticleActions.Unknown;
            if (dr.Table.Columns[alias] == null) return ArticleActions.Unknown;

            if (dr == null) return ArticleActions.Unknown;
            if (dr[alias] != DBNull.Value) return (ArticleActions)Enum.Parse(typeof(ArticleActions), dr[alias].ToString());

            return ArticleActions.Unknown;
        }
    }
}
