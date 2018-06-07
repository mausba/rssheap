using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using Core.Data;

namespace System.Data
{
    public static class DataSetExtensions
    {
        public static List<KeyValuePair<string, string>> ToKeyValuePairs(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToKeyValuePair);
        }

        public static List<UserTaggedArticle> ToUserTaggedArticles(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToUserTaggedArticle);
        }

        public static List<Feed> ToFeeds(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToFeed);
        }

        public static List<User> ToUsers(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToUser);
        }

        public static List<int> ToListOfIds(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToId);
        }

        public static List<Vote> ToArticleVotes(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToArticleVote);
        }

        public static List<Tag> ToTags(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToTag);
        }

        public static List<ArticleTag> ToArticleTags(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToArticleTag);
        }

        public static List<Payment> ToPayments(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToOrder);
        }

        public static List<UserFeed> ToUserFeeds(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToUserFeed);
        }

        public static List<Folder> ToFolders(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToFolder);
        }

        public static List<Article> ToArticles(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToArticle);
        }

        public static List<OPML> ToOPML(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToOPML);
        }

        public static List<Article> ToArticlesWithAssObjects(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToArticleWithAssObjects);
        }

        public static List<FavoriteArticle> ToFavoriteArticles(this DataSet ds)
        {
            return ConvertDataSetToList(ds, DataConverter.ToFavoriteArticle);
        }

        public static int ToIdentity(this DataSet ds)
        {
            if (ds.Tables.Count == 0) return 0;
            if (ds.Tables[0].Rows.Count == 0) return 0;

            return Convert.ToInt32(ds.Tables[0].Rows[0][0]);
        }

        public static List<T> ConvertDataSetToList<T>(DataSet ds, Converter<DataRow, T> conversion)
        {
            List<T> retval = new List<T>();
            if (ds == null) return retval;

            foreach (DataRow dr in ds.Tables[0].Rows)
                retval.Add(conversion(dr));

            return retval;
        }
    }
}
