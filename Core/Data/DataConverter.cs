using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using System.Data;
using Core.Enums;

namespace Core.Data
{
    public static class DataConverter
    {
        public static KeyValuePair<string, string> ToKeyValuePair(DataRow dr)
        {
            return new KeyValuePair<string, string>(dr.ToStringOrNull("key"), dr.ToStringOrNull("value"));
        }

        public static User ToUser(DataRow dr)
        {
            var u = new User
            {
                Id = dr.ToIntOrZero("Id"),
                UserName = dr.ToStringOrNull("UserName"),
                Salt = dr.ToBytesOrNull("Salt"),
                Password = dr.ToBytesOrNull("Password"),
                Email = dr.ToStringOrNull("Email"),
                FirstName = dr.ToStringOrNull("FirstName"),
                LastName = dr.ToStringOrNull("LastName"),
                Summary = dr.ToStringOrNull("Summary"),
                Following = dr.ToIntOrZero("Following"),
                Followers = dr.ToIntOrZero("Followers"),
                FollowingUserIds = dr.ToCommaSeparatedListOfIds("FollowingUserIds"),
                FavoriteTagIds = dr.ToCommaSeparatedListOfIds("TagIds"),
                IgnoredTagIds = dr.ToCommaSeparatedListOfIds("IgnoredTagIds"),
                ProfilePhoto = dr.ToStringOrNull("ImageUrl"),
                Created = dr.ToDateTimeOrMinValue("Created"),
                Reputation = dr.ToIntOrZero("Reputation"),
                HideVisitedArticles = dr.ToBoolOrFase("HideVisitedArticles"),
                IsAdmin = dr.ToBoolOrFase("IsAdmin"),
                RemoteId = dr.ToStringOrNull("RemoteId"),
                GUID = dr.ToStringOrNull("GUID"),
                Subscribed = dr.ToBoolOrFase("Subscribed"),
                HideOlderThan = dr.ToStringOrNull("HideOlderThan"),
                LastSeen = dr.ToDateTimeOrMinValue("LastSeen"),
                SharedOnFacebook = dr.ToBoolOrFase("SharedOnFacebook"),
                SharedOnTwitter = dr.ToBoolOrFase("SharedOnTwitter")
            };
            u.BadgeIds.AddRange(dr.ToCommaSeparatedListOfIds("BadgeIds"));
            var loginProvider = dr.ToStringOrNull("LoginProvider");
            u.LoginProvider = !string.IsNullOrEmpty(loginProvider) ? (LoginProvider)Enum.Parse(typeof(LoginProvider), loginProvider) : LoginProvider.Internal;
            return u;
        }

        public static UserTaggedArticle ToUserTaggedArticle(DataRow dr)
        {
            var uta = new UserTaggedArticle
            {
                Approved = dr.ToBoolOrFase("Approved"),
                ArticleId = dr.ToIntOrZero("ArticleId"),
                TagId = dr.ToIntOrZero("TagId")
            };
            return uta;
        }

        public static Feed ToFeed(DataRow dr)
        {
            return ToFeed(dr, null);
        }

        private static Feed ToFeed(DataRow dr, string prefix)
        {
            Feed f = new Feed
            {
                Id = dr.ToIntOrZero(prefix + "Id"),
                Author = dr.ToStringOrNull(prefix + "Author"),
                Name = dr.ToStringOrNull(prefix + "Name"),
                Description = dr.ToStringOrNull(prefix + "Descriptions"),
                Url = dr.ToStringOrNull(prefix + "Url"),
                SiteUrl = dr.ToStringOrNull(prefix + "SiteUrl"),
                Created = dr.ToDateTimeOrMinValue(prefix + "Created"),
                Updated = dr.ToDateTimeOrMinValue(prefix + "Updated"),
                TotalArticles = dr.ToIntOrZero(prefix + "TotalArticles"),
                TotalLikes = dr.ToIntOrZero(prefix + "TotalLikes"),
                TotalViews = dr.ToIntOrZero(prefix + "TotalViews"),
                Public = dr.ToBoolOrFase(prefix + "Public"),
                Reviewed = dr.ToBoolOrFase(prefix + "Reviewed")
            };
            return f;
        }

        internal static Article ToArticleWithAssObjects(DataRow dr)
        {
            var article = ToArticle(dr, "Article.");
            article.Feed = ToFeed(dr, "Feed.");

            article.Tags = (dr.ToStringOrNull("Tag.Name") ?? string.Empty)
                             .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Distinct()
                             .Select(t => new Tag
                             {
                                 Name = t
                             }).ToList();

            article.MyVotes = dr.ToIntOrZero("Votes");
            article.IsMyFavorite = dr.ToIntOrZero("MyFavoriteId") > 0;

            return article;
        }

        public static Article ToArticle(DataRow dr)
        {
            return ToArticle(dr, null);
        }

        private static Article ToArticle(DataRow dr, string prefix)
        {
            Article fe = new Article
            {
                Id = dr.ToIntOrZero(prefix + "Id"),
                FeedId = dr.ToIntOrZero(prefix + "FeedId"),
                Name = dr.ToStringOrNull(prefix + "Name"),
                Body = dr.ToStringOrNull(prefix + "Body"),
                Url = dr.ToStringOrNull(prefix + "Url"),
                ViewsCount = dr.ToIntOrZero(prefix + "ViewsCount"),
                LikesCount = dr.ToIntOrZero(prefix + "LikesCount"),
                FavoriteCount = dr.ToIntOrZero(prefix + "FavoriteCount"),
                Published = dr.ToDateTimeOrMinValue(prefix + "Published"),
                Created = dr.ToDateTimeOrMinValue(prefix + "Created"),
                ShortUrl = dr.ToStringOrNull(prefix + "ShortUrl"),
                Flagged = dr.ToBoolOrFase(prefix + "Flagged"),
                FlaggedBy = dr.ToCommaSeparatedListOfIds(prefix + "FlaggedBy")
            };
            return fe;
        }

        public static ArticleTag ToArticleTag(DataRow dr)
        {
            var at = new ArticleTag();
            at.ArticleId = dr.ToIntOrZero("ArticleId");
            at.TagName = dr.ToStringOrNull("TagName");
            return at;
        }

        public static Tag ToTag(DataRow dr)
        {
            var c = new Tag
            {
                Id = dr.ToIntOrZero("Id"),
                Name = dr.ToStringOrNull("Name"),
                Description = dr.ToStringOrNull("Description"),
                ArticlesCount = dr.ToIntOrZero("ArticlesCount"),
                Active = dr.ToBoolOrFase("Active"),
                SynonimTagId = dr.ToIntOrZero("SynonimTagId"),
                SimilarTagIds = dr.ToCommaSeparatedListOfIds("SimilarTagIds"),
                Approved = dr.ToBoolOrFase("Approved"),
                ApprovedBy = dr.ToCommaSeparatedListOfIds("ApprovedBy"),
                RejectedBy = dr.ToCommaSeparatedListOfIds("RejectedBy"),
                Created = dr.ToDateTimeOrMinValue("Date"),
                SubscribersCount = dr.ToIntOrZero("SubscribersCount"),
                MatchTitleOnly = dr.ToBoolOrFase("MatchTitleOnly")
            };
            return c;
        }

        internal static Vote ToArticleVote(DataRow dr)
        {
            var ev = new Vote
            {
                Id = dr.ToIntOrZero("Id"),
                UserId = dr.ToIntOrZero("UserId"),
                ArticleId = dr.ToIntOrZero("ArticleId"),
                Votes = dr.ToIntOrZero("Votes")
            };
            return ev;
        }

        internal static FavoriteArticle ToFavoriteArticle(DataRow dr)
        {
            var fe = new FavoriteArticle
            {
                Id = dr.ToIntOrZero("Id"),
                UserId = dr.ToIntOrZero("UserId"),
                ArticleId = dr.ToIntOrZero("ArticleId")
            };
            return fe;
        }

        internal static UserFeed ToUserFeed(DataRow dr)
        {
            var uf = new UserFeed
            {
                Id = dr.ToIntOrZero("Id"),
                UserId = dr.ToIntOrZero("UserId"),
                Submited = dr.ToBoolOrFase("Submited"),
                FeedId = dr.ToIntOrZero("FeedId"),
                Subscribed = dr.ToBoolOrFase("Subscribed"),
                Ignored = dr.ToBoolOrFase("Ignored")
            };
            return uf;
        }

        internal static int ToId(DataRow dr)
        {
            return dr.ToIntOrZero("Id");
        }

        internal static Folder ToFolder(DataRow dr)
        {
            if (dr == null) return null;
            var f = new Folder
            {
                Id = dr.ToIntOrZero("Id"),
                Name = dr.ToStringOrNull("Name"),
                Description = dr.ToStringOrNull("Description"),
                UserId = dr.ToIntOrZero("UserId")
            };
            return f;
        }

        internal static OPML ToOPML(DataRow dr)
        {
            var o = new OPML
            {
                Id = dr.ToIntOrZero("Id"),
                UserId = dr.ToIntOrZero("UserId"),
                FileName = dr.ToStringOrNull("FileName"),
                Parsed = dr.ToBoolOrFase("Parsed"),
                Date = dr.ToDateTimeOrMinValue("Date")
            };
            return o;
        }

        internal static Payment ToOrder(DataRow dr)
        {
            var o = new Payment
            {
                Id = dr.ToIntOrZero("Id"),
                UserId = dr.ToIntOrZero("UserId"),
                TransactionId = dr.ToStringOrNull("TransactionId"),
                OrderType = dr.ToStringOrNull("OrderType"),
                Amount = Convert.ToDecimal(dr["Amount"]),
                Email = dr.ToStringOrNull("Email"),
                Date = dr.ToDateTimeOrMinValue("Date")
            };
            return o;
        }
    }
}
