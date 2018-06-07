using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using Core.Data;
using Core.Enums;
using System.Net.Mail;
using System.Globalization;

namespace Core.Models
{
    public class User
    {
        public User()
        {
            FavoriteTagIds = new List<int>();
            Tags = new List<Tag>();
            IgnoredTagIds = new List<int>();
            IgnoredTags = new List<Tag>();
            BadgeIds = new List<int>();
            FollowingUserIds = new List<int>();
            MyFeeds = new List<UserFeed>();
            LoginProvider = LoginProvider.Internal;
            Folders = new List<Folder>();
            LastSeen = DateTime.Now;
        }

        public int Id { get; set; }
        public string RemoteId { get; set; }
        public LoginProvider LoginProvider { get; set; }
        public string UserName { get; set; }
        public byte[] Salt { get; set; }
        public byte[] Password { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Summary { get; set; }
        public int Following { get; set; }
        public int Followers { get; set; }
        public string ProfilePhoto { get; set; }
        public DateTime Created { get; set; }
        public bool IsAdmin { get; set; }
        public string GUID { get; set; }
        public DateTime LastSeen { get; set; }

        public bool SharedOnFacebook { get; set; }
        public bool SharedOnTwitter { get; set; }

        public bool HideVisitedArticles { get; set; }
        public string HideOlderThan { get; set; }
        public DateTime HideOlderThanDateTime
        {
            get
            {
                if (!HideOlderThan.IsNullOrEmpty())
                {
                    int months = int.TryParse(HideOlderThan, out months) ? months : 0;
                    if (months > 0)
                    {
                        return DateTime.Now.AddMonths(-months);
                    }
                    else
                    {
                        try
                        {
                            DateTime d = DateTime.ParseExact(HideOlderThan, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                            return d;
                        }
                        catch { }
                    }
                }
                return DateTime.MinValue;
            }
        }

        public List<int> FollowingUserIds { get; set; }
        public List<int> BadgeIds { get; set; }
        public List<int> FavoriteTagIds { get; set; }
        public List<Tag> Tags { get; set; }

        public List<int> IgnoredTagIds { get; set; }
        public List<Tag> IgnoredTags { get; set; }

        public List<UserFeed> MyFeeds { get; set; }
        public List<int> FavoriteFeedIds
        {
            get
            {
                return MyFeeds.Where(f => (f.Subscribed || f.Submited) && !f.Ignored).Select(f => f.FeedId).ToList();
            }
        }
        public List<int> IgnoredFeedIds 
        {
            get
            {
                return MyFeeds.Where(f => !f.Subscribed && f.Ignored).Select(f => f.FeedId).ToList();
            }
        }

        private static Random _rnd = new Random();
        public string RandomTag
        {
            get
            {
                if (Tags == null || Tags.Count == 0) return null;
                return Tags[_rnd.Next(0, Tags.Count - 1)].Name;
            }
        }

        public int Reputation { get; set; }

        public bool CanAddNewTag { get { return Reputation >= 2000 || IsAdmin; } }
        public bool CanRemoveEntryTag { get { return Reputation >= 4000 || IsAdmin; } }

        public bool Subscribed { get; set; }

        private string _Email;
        public string GetEmailAddress()
        {
            if (_Email == null)
            {
                var email = Email;
                if (email.IsNullOrEmpty())
                    email = UserName;
                try
                {
                    new MailAddress(email);
                }
                catch
                {
                    _Email = string.Empty;
                }
                _Email = email;
            }
            return _Email;
        }

        public List<Folder> Folders { get; set; }
    }
}
