using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using Core.Data;
using Core.Enums;
using Core.Caching;

namespace Core.Services
{
    public class UserService
    {
        private readonly DataProvider DataProvider = new DataProvider();

        public int GetNoOfUsers()
        {
            return DataProvider.GetNoOfUsers();
        }

        public User GetUser(int id)
        {
            if (id <= 0) throw new Exception("Invalid id");
            return DataProvider.GetUser(id);
        }

        public User GetUserCached(string id, Action<User> afterLoad)
        {
            return CacheClient.Default.GetOrAdd("user" + id, CachePeriod.ForMinutes(15),
                () =>
                {
                    var user = GetUser(int.Parse(id));
                    afterLoad?.Invoke(user);
                    return user;
                });
        }

        public void UpdateUser(User user)
        {
            DataProvider.UpdateUser(user);
        }

        public User GetUser(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName)) throw new Exception("UserName empty");
            if (string.IsNullOrEmpty(password)) throw new Exception("Password empty");

            return DataProvider.GetUser(userName, password);
        }

        public void DeleteUser(int userId)
        {
            DataProvider.DeleteUser(userId);
        }

        public int InsertUser(User user, Action onComplete = null)
        {
            user.Id = DataProvider.InsertUser(user);
            onComplete?.Invoke();
            return user.Id;
        }

        public void UpdateLastSeen(User user)
        {
            if (user == null) return;
            try
            {
                if (user.LastSeen > DateTime.MinValue && user.LastSeen < DateTime.Now.AddDays(-1))
                {
                    user.Reputation += 1;
                    DataProvider.UpdateUser(user);
                }
                DataProvider.UpdateLastSeen(user.Id);
            }
            catch { }
        }

        public void FollowUser(int userId, int toFollowUserId)
        {
            var user = GetUser(userId);
            if (user == null) throw new Exception("User not found");
            if (user.FollowingUserIds.Contains(toFollowUserId)) return;

            var followUser = GetUser(toFollowUserId);
            if (followUser == null) throw new Exception("User to follow not found");
            followUser.Followers++;
            UpdateUser(followUser);

            user.FollowingUserIds.Add(toFollowUserId);
            UpdateUser(user);
        }

        public void UpdateUserSettings(int id, string email, string firstName, string lastName, string summary, string profilePhoto)
        {
            var user = GetUser(id);
            if (user == null) throw new Exception("User not found");

            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Summary = summary;
            user.ProfilePhoto = profilePhoto;
            UpdateUser(user);
        }

        public List<Vote> GetUserVotes(int userId)
        {
            return DataProvider.GetUserVotes(userId);
        }

        public int InsertUserFeed(UserFeed userFeed)
        {
            return DataProvider.InsertUserFeed(userFeed);
        }

        public List<UserFeed> GetUserFeeds(int userId)
        {
            return DataProvider.GetUserFeeds(userId);
        }

        public List<Feed> GetUserFavoriteFeeds(int userId)
        {
            return DataProvider.GetUserFavoriteFeeds(userId);
        }

        public void UpdateUserFeed(UserFeed userFeed)
        {
            DataProvider.UpdateUserFeed(userFeed);
        }

        public User GetUser(string remoteId, LoginProvider loginProvider)
        {
            if (loginProvider == LoginProvider.Internal)
                return DataProvider.GetUser(int.Parse(remoteId));

            return DataProvider.GetUser(remoteId, loginProvider);
        }

        public User GetUser(string userName)
        {
            if (userName.IsNullOrEmpty()) return null;
            userName = userName.ToLower();
            return DataProvider.GetUser(userName);
        }

        public User GetUserByGuid(string guid)
        {
            if (guid.IsNullOrEmpty()) return null;
            return DataProvider.GetUserByGuid(guid);
        }

        public void InsertNewsletterSent(DateTime dateTime, int userId, string userEmail)
        {
            DataProvider.InsertNewsletterSent(dateTime, userId, userEmail);
        }

        public List<User> GetUsersSubscribedToNewsletters()
        {
            return DataProvider.GetUsersSubscribedToNewsletters();
        }

        public List<DateTime> GetUserNewsletterDates(int userId)
        {
            return DataProvider.GetUserNewsletterDates(userId);
        }

        public List<Tuple<int, DateTime>> GetUserNewsletterDates(IEnumerable<int> customerIds)
        {
            return DataProvider.GetUserNewsletterDates(customerIds);
        }

        public List<Folder> GetUserFolders(int userId)
        {
            return DataProvider.GetUserFolders(userId);
        }

        public Folder GetUserFolder(int userId, string name)
        {
            return DataProvider.GetUserFolder(userId, name);
        }

        public void DeleteUserFolder(int userId, int folderId)
        {
            DataProvider.DeleteUserFolder(userId, folderId);
        }

        public Folder InsertUserFolder(int userId, Folder folder)
        {
            folder.Id = DataProvider.InsertUserFolder(userId, folder);
            return folder;
        }

        public List<Folder> GetUserFolderAvailableForFeed(int userId, int feedId)
        {
            return DataProvider.GetUserFolderAvailableForFeed(userId, feedId);
        }

        public void InsertUserFeedFolder(int userId, int feedId, int folderId)
        {
            DataProvider.InsertUserFeedFolder(userId, feedId, folderId);
        }

        public Folder GetUserFolder(int userId, int folderId)
        {
            return DataProvider.GetUserFolder(userId, folderId);
        }

        public object GetUserFoldersForFeed(int userId, int feedId)
        {
            return DataProvider.GetUserFoldersForFeed(userId, feedId);
        }

        public void DeleteUserFeedFromFolder(int userId, int folderId, int feedId)
        {
            DataProvider.DeleteUserFeedFromFolder(userId, folderId, feedId);
        }

        public void UpdateUserPassword(int userId, byte[] salt, byte[] hash)
        {
            DataProvider.UpdateUserPassword(userId, salt, hash);
        }
        public List<User> GetAllUsers()
        {
            return DataProvider.GetAllUsers();
        }
    }
}
