using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Models;
using Core.Data;
using Core.Enums;
using Core.Models.Requests;
using Core.Models.Responses;
using System.Net;
using System.IO;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Xml.Linq;
using Core.Utilities;
using HtmlAgilityPack;

namespace Core.Services
{
    public class FeedService
    {
        private readonly DataProvider DataProvider = new DataProvider();

        public Feed GetFeed(int id)
        {
            return DataProvider.GetFeed(id);
        }

        public void InsertArticleAsIgnored(int articleId, User user)
        {
            if (user == null) return;
            DataProvider.InsertArticleAsIgnored(articleId, user.Id);
        }

        public void InsertFeedAsIgnored(int feedId, User user)
        {
            DataProvider.InsertFeedAsIgnored(feedId, user.Id);
        }

        public List<Tag> GetTags()
        {
            return DataProvider.GetTags();
        }

        public List<Tag> GetTagsContaining(string chars, int count)
        {
            if (chars.IsNullOrEmpty()) return new List<Tag>();
            return DataProvider.GetTagsContaining(chars, count);
        }

        public List<Feed> GetFeeds()
        {
            return DataProvider.GetFeeds();
        }

        public void InsertOPML(int userId, string fileName)
        {
            DataProvider.InsertOPML(userId, fileName);
        }

        public List<OPML> GetOPMLFilesToParse()
        {
            return DataProvider.GetOPMLFilesToParse();
        }

        public void UpdateOPMLAsParsed(int id)
        {
            DataProvider.UpdateOPMLAsParsed(id);
        }

        public void DeleteFeed(int id)
        {
            DataProvider.DeleteFeed(id);
        }

        public int InsertFeed(Feed feed)
        {
            if (string.IsNullOrEmpty(feed.Url)) throw new Exception("Url missing");

            return DataProvider.InsertFeed(feed);
        }

        public int InsertArticle(Article article)
        {
            if (string.IsNullOrEmpty(article.Name)) throw new Exception("Name missing");
            if (string.IsNullOrEmpty(article.Url)) throw new Exception("Url missing");
            if (article.FeedId == 0) throw new Exception("FeedId missing");

            return DataProvider.InsertArticle(article);
        }

        public List<Article> GetArticlesWithoutBody(int feedId)
        {
            return DataProvider.GetArticlesWithoutBody(feedId);
        }

        public Article GetArticle(int id)
        {
            return DataProvider.GetArticle(id);
        }

        public Article GetArticleWithAssObjects(int id, User user)
        {
            return DataProvider.GetArticleWithAssObjects(id, user);
        }

        public List<Article> GetArticles()
        {
            return DataProvider.GetArticles();
        }

        public void UpdateArticle(Article article)
        {
            DataProvider.UpdateArticle(article);
        }

        public void DeleteArticle(int id)
        {
            DataProvider.DeleteArticle(id);
        }

        public void UpdateFeed(Feed feed)
        {
            DataProvider.UpdateFeed(feed);
        }

        public void UpdateArticleAsRead(int userId, int articleId)
        {
            DataProvider.UpdateArticleAsRead(userId, articleId);
        }

        public Feed GetFeedBySiteUrl(string url)
        {
            if (url.IsNullOrEmpty()) return null;
            return DataProvider.GetFeed(url);
        }

        public Feed GetFeedByXmlUrl(string rssUrl)
        {
            if (rssUrl.IsNullOrEmpty()) return null;
            return DataProvider.GetFeedByXmlUrl(rssUrl);
        }

        public HttpWebRequest CreateRequest(string url)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Headers["Accept-Encoding"] = "gzip,deflate";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.KeepAlive = true;
            request.Timeout = 10000;
            request.MaximumAutomaticRedirections = 3;
            request.MaximumResponseHeadersLength = 1024 * 64 * 64;
            return request;
        }

        public Feed GetRemoteFeed(Feed lFeed, int timeout = 0)
        {
            string url = lFeed.Url;
            string siteUrl = lFeed.SiteUrl;

            var remoteFeed = new Feed
            {
                Url = url
            };

            SyndicationFeed rss = null;
            XmlReader reader = null;
            var encoding = GetEncoding(url, siteUrl);

            try
            {
                var str = string.Empty;
                using (var wc = new WebClient())
                {
                    remoteFeed.Encoding = encoding;
                    wc.Headers["User-Agent"] = "www.rssheap.com";
                    wc.Encoding = encoding;
                    str = wc.DownloadString(url);
                }

                str = str.Replace("media:thumbnail", "media");  //mashable fix

                reader = new MyXmlReader(new StringReader(str), new XmlReaderSettings
                {
                    //MaxCharactersInDocument can be used to control the maximum amount of data 
                    //read from the reader and helps prevent OutOfMemoryException
                    MaxCharactersInDocument = 1024 * 64 * 64 * 64,
                    DtdProcessing = DtdProcessing.Parse
                });

                rss = SyndicationFeed.Load(reader);
            }
            catch
            {
                try
                {
                    var rss10ff = new Rss10FeedFormatter();
                    rss10ff.ReadFrom(reader);
                    rss = rss10ff.Feed;
                }
                catch
                {
                    return null;
                }
            }

            if (rss == null || rss.Items == null) return null;

            string authorName = (rss.Authors != null && rss.Authors.Count > 0) ? rss.Authors.First().Name ?? rss.Authors.First().Email : string.Empty;
            remoteFeed.Author = authorName;

            string title = rss.Title != null ? rss.Title.Text : string.Empty;
            remoteFeed.Name = title;

            string description = rss.Description != null ? rss.Description.Text : string.Empty;
            remoteFeed.Description = description;

            //get the site url
            if (rss.Links != null)
            {
                var siteLink = rss.Links.FirstOrDefault(l => l.RelationshipType == "alternate");
                if (siteLink != null)
                {
                    try
                    {
                        remoteFeed.SiteUrl = siteLink.Uri.AbsoluteUri;
                    }
                    catch { }
                }
                if (remoteFeed.SiteUrl.IsNullOrEmpty() && rss.Links.Count > 0)
                {
                    try
                    {
                        remoteFeed.SiteUrl = rss.Links.First().Uri.AbsoluteUri;
                    }
                    catch { }
                }
            }

            if (remoteFeed.SiteUrl.IsNullOrEmpty())
            {
                try
                {
                    remoteFeed.SiteUrl = new Uri(rss.Id).AbsoluteUri;
                }
                catch { }
            }

            if (remoteFeed.SiteUrl.IsNullOrEmpty() && !siteUrl.IsNullOrEmpty())
            {
                remoteFeed.SiteUrl = siteUrl;

                //find canonical
                var html = string.Empty;
                using (var wc = new WebClient())
                {
                    wc.Encoding = encoding;
                    html = wc.DownloadString(siteUrl);

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(html);
                    var canonical = htmlDocument.DocumentNode.SelectNodes("//link")
                                                .Where(s => s.GetAttributeValue("rel", false) &&
                                                            s.GetAttributeValue("href", false) &&
                                                            s.GetAttributeValue("rel", string.Empty) == "canonical")
                                                .Select(s => s.GetAttributeValue("href", string.Empty))
                                                .FirstOrDefault(c => !c.IsNullOrEmpty());

                    if (!canonical.IsNullOrEmpty())
                    {
                        if (canonical != "/")
                            remoteFeed.SiteUrl = canonical;
                        else
                            remoteFeed.SiteUrl = siteUrl;
                    }
                }
            }

            if (remoteFeed.SiteUrl.IsNullOrEmpty())
            {
                try
                {
                    remoteFeed.SiteUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
                }
                catch
                {

                }
            }

            foreach (var item in rss.Items)
            {
                try
                {
                    var uri = item.Links[0].Uri;
                    string itemUrl = string.Empty;
                    if (uri.IsAbsoluteUri)
                    {
                        if (item.Links.Any(t => t.MediaType.IsNullOrEmpty()))
                        {
                            itemUrl = item.Links.First(t => t.MediaType.IsNullOrEmpty()).Uri.AbsoluteUri;
                        }
                        else
                        {
                            itemUrl = item.Links[0].Uri.AbsoluteUri;
                        }
                    }
                    else
                    {
                        itemUrl = new Uri(new Uri(remoteFeed.SiteUrl), uri.ToString()).AbsoluteUri;
                    }

                    if (itemUrl.IsNullOrEmpty()) continue;

                    string itemBody = string.Empty;

                    Article feedItem = new Article();
                    itemBody = item.Summary != null ? item.Summary.Text : string.Empty;

                    if (item.ElementExtensions != null)
                    {
                        string bodyTemp = item.ElementExtensions
                                              .ReadElementExtensions<string>("encoded", "http://purl.org/rss/1.0/modules/content/")
                                              .FirstOrDefault();
                        if (bodyTemp != null) itemBody = bodyTemp;
                    }

                    if (itemBody.IsNullOrEmpty())
                    {
                        if (item.Content != null)
                        {
                            if (item.Content is TextSyndicationContent textContent)
                                itemBody = textContent.Text;
                        }
                    }

                    feedItem.Name = item.Title != null ? item.Title.Text : string.Empty;
                    if (feedItem.Name.IsNullOrEmpty())
                    {
                        feedItem.Name = itemUrl;
                        if (itemUrl.IndexOf("/") > 0)
                        {
                            var lastIndex = itemUrl.LastIndexOf("/") + 1;
                            var strippedUrl = itemUrl.Substring(lastIndex, itemUrl.Length - lastIndex).Replace("-", " ");
                            if (strippedUrl.IndexOf("-") > 0)
                            {
                                feedItem.Name = strippedUrl;
                            }
                        }
                    }

                    if (!feedItem.Name.IsNullOrEmpty())
                        feedItem.Name = feedItem.Name.Trim();

                    if (item.Authors.Count > 0)
                    {
                        remoteFeed.Author = item.Authors[0].Name ?? item.Authors[0].Email;
                    }
                    feedItem.Url = itemUrl;

                    //if it is feedburner get the url from <feedburner:origLink>
                    var elemExt = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "origLink");
                    if (elemExt != null)
                    {
                        feedItem.Url = elemExt.GetObject<XElement>().Value;
                    }

                    feedItem.Body = itemBody;
                    feedItem.Published = item.PublishDate.DateTime != DateTime.MinValue ? item.PublishDate.DateTime : item.LastUpdatedTime.DateTime;
                    if (feedItem.Published == DateTime.MinValue)
                    {
                        try
                        {
                            var date = item.ElementExtensions.FirstOrDefault(e => e.OuterName.ToLower().Contains("date")).GetObject<XElement>().Value;
                            feedItem.Published = DateTime.Parse(date);
                        }
                        catch { }
                    }

                    if (feedItem.Published == DateTime.MinValue)
                    {
                        try
                        {
                            var blogPubDate = rss.ElementExtensions.FirstOrDefault(e => e.OuterName.ToLower().Contains("date")).GetObject<XElement>().Value;
                            feedItem.Published = DateTime.Parse(blogPubDate);
                        }
                        catch
                        {
                            if (rss.LastUpdatedTime.Date > DateTime.MinValue)
                                feedItem.Published = rss.LastUpdatedTime.Date;
                        }
                    }

                    if (remoteFeed.SiteUrl.Contains("echojs.com") && feedItem.Published == DateTime.MinValue)
                        feedItem.Published = DateTime.Now;

                    if (feedItem.Published == DateTime.MinValue && lFeed.Public)
                        feedItem.Published = DateTime.Now;

                    remoteFeed.Articles.Add(feedItem);
                }
                catch (Exception ex)
                {
                    new LogService().InsertError(ex.ToString(), "GetRemoteRss FeedUrl: " + url);
                }
            }
            return remoteFeed;
        }

        public void FlagArticle(int articleId)
        {
            DataProvider.FlagArticle(articleId);
        }

        private Encoding GetEncoding(string rssUrl, string siteUrl)
        {
            var encoding = Encoding.UTF8;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(rssUrl);
                var res = (HttpWebResponse)req.GetResponse();
                try
                {
                    encoding = Encoding.GetEncoding(res.CharacterSet);
                }
                catch
                {
                    //get from xml
                    var loadedFromXml = false;
                    try
                    {
                        using (var wc = new WebClient())
                        {
                            var str = wc.DownloadString(rssUrl);
                            var xml = new XmlDocument();
                            xml.LoadXml(str);
                            if (xml.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
                            {
                                encoding = Encoding.GetEncoding(((XmlDeclaration)xml.FirstChild).Encoding);
                                loadedFromXml = true;
                            }
                        }
                    }
                    catch
                    {

                    }

                    if (!loadedFromXml && !siteUrl.IsNullOrEmpty())
                    {
                        req = (HttpWebRequest)WebRequest.Create(siteUrl);
                        res = (HttpWebResponse)req.GetResponse();
                        try
                        {
                            encoding = Encoding.GetEncoding(res.CharacterSet);
                        }
                        catch { }
                    }
                }
                finally
                {
                    res.Close();
                }

            }
            catch
            {

            }
            return encoding;
        }

        public List<Feed> GetFeeds(List<int> ids)
        {
            if (ids.Count == 0)
            {
                return new List<Feed>();
            }
            return DataProvider.GetFeeds(ids);
        }

        internal void IncreaseArticleCommentsCount(int articleId)
        {
            DataProvider.IncreaseArticleCommentsCount(articleId);
        }

        public List<Article> GetArticles(ArticlesRequest request)
        {
            return DataProvider.GetArticles(request);
        }

        public void AddSearch(string search, User user)
        {
            DataProvider.AddSearch(search, user);
        }

        public int GetUserSearchesForDate(User user)
        {
            return DataProvider.GetUserSearchesForDate(user.Id, DateTime.Now);
        }

        internal List<Article> InsertArticles(List<Article> articles)
        {
            foreach (var e in articles)
            {
                e.Id = InsertArticle(e);
            }
            return articles;
        }

        public List<FavoriteArticle> GetFavoriteArticlesForUser(int userId)
        {
            return DataProvider.GetFavoriteArticlesForUser(userId);
        }

        public List<Tag> GetTags(List<string> names)
        {
            return DataProvider.GetTags(names);
        }

        public List<Tag> GetTagsWithSimilarAndSynonims(List<int> tagIds)
        {
            return DataProvider.GetTagsWithSimilarAndSynonims(tagIds);
        }

        public void InsertFavoriteArticle(User user, int articleId)
        {
            InsertFavoriteArticle(user.Id, articleId);
        }

        public void InsertFavoriteArticle(int userId, int articleId)
        {
            DataProvider.InsertFavoriteArticle(userId, articleId);
        }

        public void DeleteFavoriteArticle(User user, int articleId)
        {
            DataProvider.DeleteFavoriteArticle(user.Id, articleId);
        }

        public void UpVote(User user, int articleId)
        {
            var vote = 1;
            AddUpdateArticleVote(user, articleId, vote);
        }

        public void DownVote(User user, int articleId)
        {
            var vote = -1;
            AddUpdateArticleVote(user, articleId, vote);
        }

        private void AddUpdateArticleVote(User user, int articleId, int vote)
        {
            var article = new FeedService().GetArticle(articleId);
            if (article == null) return;
            var newVote = new Vote
            {
                ArticleId = article.Id,
                UserId = user.Id,
                Votes = vote
            };
            DataProvider.InsertArticleVote(newVote);
        }

        public Tag InsertTag(Tag tag)
        {
            var newId = DataProvider.InsertTag(tag);
            tag.Id = newId;
            return tag;
        }

        public void UpdateTagArticleCount(int tagId)
        {
            DataProvider.UpdateTagArticleCount(tagId);
        }

        public List<Tag> GetTags(List<int> tagIds)
        {
            return DataProvider.GetTags(tagIds);
        }

        public Tag GetTag(int id)
        {
            return DataProvider.GetTag(id);
        }

        public Tag GetTag(string name)
        {
            return DataProvider.GetTag(name);
        }

        public void UpdateTag(Tag tag)
        {
            DataProvider.UpdateTag(tag);
        }

        public void UpdateTagAndArticleCount(Tag tag)
        {
            tag.SimilarTagIds = tag.SimilarTagIds.Distinct().ToList();
            DataProvider.UpdateTagArticleCount(tag.Id);
            DataProvider.UpdateTag(tag);
        }

        public void DeleteArticleTag(int articleId, string tagName)
        {
            var article = GetArticle(articleId);
            if (article == null) return;

            DataProvider.DeleteArticleTag(articleId, tagName);
        }

        //TODO refactor: do not use TagResponse, create another object
        public TagResponse RejectTag(int articleId, string tagName, User user)
        {
            if (user == null) return null;
            var rejected = false;

            string error = string.Empty;
            if (tagName.IsNullOrEmpty()) return null;
            tagName = tagName.ToTagName();

            var article = GetArticle(articleId);
            if (article == null) return null;

            var tag = GetTag(tagName);
            if (tag == null) return null;

            article.Tags = GetArticleTags(article.Id);

            var at = article.Tags.FirstOrDefault(t => t.Name == tagName);
            if (at.RejectedBy.Contains(user.Id)) return null;

            if (at.RejectedBy.Count >= 3 || user.CanRemoveEntryTag)
            {
                DeleteArticleTag(articleId, tagName);
                IISTaskManager.Run(() =>
                {
                    //give user that suggested category some reputation
                    //prevent user to add reputation to himself by removing and than adding 
                    //the same tag over and over again (if he has auto aprove permision)
                    if (at.RejectedBy.Count > 0)
                    {
                        var userId = at.RejectedBy.First();
                        var userToAddReputation = userId != user.Id ? new UserService().GetUser(userId) : user;

                        if (userToAddReputation != null)
                        {
                            userToAddReputation.Reputation += 5;
                            rejected = true;
                            new UserService().UpdateUser(userToAddReputation);
                        }
                    }
                });
            }
            else
            {
                at.RejectedBy.Add(user.Id);
                UpdateArticleTag(articleId, at);
            }
            return new TagResponse
            {
                TagId = tag.Id,
                TagName = tag.Name,
                Article = article,
                Approved = rejected
            };
        }

        public TagResponse SuggestTag(int articleId, string tagName, User user)
        {
            if (user == null) return null;

            string error = string.Empty;
            if (tagName.IsNullOrEmpty()) return new TagResponse { Error = "Tag name can not be an empty string" };
            tagName = tagName.ToTagName();

            var article = GetArticle(articleId);
            if (article == null) return null;

            var tag = GetTag(tagName);
            if (tag == null) tag = InsertTag(new Tag { Active = true, Name = tagName });

            var at = GetArticleTags(article.Id).FirstOrDefault(t => t.Name == tagName);
            if (at == null)
            {
                at = new Tag
                {
                    Id = InsertArticleTag(article, tag.Id, user)
                };
            }
            else
            {
                if (at.Approved) return new TagResponse { Error = "Tag is already added" };
                if (at.CreatedBy == user.Id) return new TagResponse { Error = "You already suggested this tag" };
                if (at.ApprovedBy.Contains(user.Id)) return new TagResponse { Error = "You already approved this tag" };
            }

            //someone else added it so check if it is ready for approval
            var approved = false;
            var needsApprovedByCount = 2 + (at.RejectedBy.Count * 2);

            if (at.ApprovedBy.Count >= needsApprovedByCount || user.CanAddNewTag)
            {
                at.ApprovedBy.Add(user.Id);
                at.Approved = true;
                UpdateArticleTag(articleId, at);

                IISTaskManager.Run(() =>
                {
                    //give user that suggested category some reputation
                    //prevent user to add reputation to himself by removing and than adding 
                    //the same tag over and over again (if he has auto aprove permision)
                    if (at.ApprovedBy.Count > 1)
                    {
                        var userId = at.CreatedBy;
                        var userToAddReputation = userId != user.Id ? new UserService().GetUser(userId) : user;

                        if (userToAddReputation != null)
                        {
                            userToAddReputation.Reputation += 5;
                            new UserService().UpdateUser(userToAddReputation);
                        }
                    }
                });

                approved = true;
            }
            else
            {
                if (at != null)
                {
                    at.ApprovedBy.Add(user.Id);
                    UpdateArticleTag(articleId, at);
                }
                else
                {
                    InsertArticleTag(article, tag.Id, user);
                }
            }
            return new TagResponse
            {
                Article = article,
                TagId = tag.Id,
                Approved = approved,
                TagName = tagName
            };
        }

        private void UpdateArticleTag(int articleId, Tag articleTag)
        {
            DataProvider.UpdateArticleTag(articleId, articleTag);
        }

        public List<Tag> GetArticleTags(int articleId)
        {
            return DataProvider.GetArticleTags(articleId);
        }

        public Tag GetArticleTag(string tagName, int articleId)
        {
            return DataProvider.GetArticleTag(tagName, articleId);
        }

        public int InsertArticleTag(Article article, int tagId, User user)
        {
            if (article == null || tagId <= 0) return 0;
            return DataProvider.InsertArticleTag(article, new Tag
                {
                    Approved = user != null ? user.CanAddNewTag : true,
                    ApprovedBy = user != null ? new List<int> { user.Id } : new List<int>(),
                    SubmittedBy = user.Id,
                    Id = tagId
                });
        }

        public List<Tag> GetTagsWithArticlesCountGreaterThan(int count)
        {
            return DataProvider.GetTagsWithArticlesCountGreaterThan(count);
        }

        public List<Tag> GetTagsForArticle(int articleId)
        {
            return DataProvider.GetTagsForArticle(articleId);
        }

        public List<int> GetTagsThatUserLikes(int userId)
        {
            if (userId <= 0) return new List<int>();
            return DataProvider.GetTagsThatUserLikes(userId);
        }

        public List<Feed> GetFeedsUpdatedBefore(bool isPublic, DateTime dateTime, int count)
        {
            return DataProvider.GetFeedsUpdatedBefore(isPublic, dateTime, count);
        }

        public List<Article> GetArticlesIndexedBefore(DateTime dateTime, int count)
        {
            return DataProvider.GetArticlesIndexedBefore(dateTime, count);
        }

        public void UpdateArticlesAsIndexed(List<int> indexed)
        {
            DataProvider.UpdateArticlesAsIndexed(indexed);
        }

        public List<Feed> GetFeedsThatAreNotParsed()
        {
            return DataProvider.GetFeedsThatAreNotParsed();
        }

        public List<Feed> GetFeedsNotPublicAndReviewed()
        {
            return DataProvider.GetFeedsNotPublicAndReviewed();
        }

        public int GetNoOfFeeds()
        {
            return DataProvider.GetNoOfFeeds();
        }

        public int GetNoOfArticles()
        {
            return DataProvider.GetNoOfArticles();
        }

        public Article GetArticle(string shorturl)
        {
            return DataProvider.GetArticle(shorturl);
        }

        public List<Feed> GetFeedsByUserFolder(int userId, int folderId)
        {
            return DataProvider.GetFeedsByUserFolder(userId, folderId);
        }

        internal void DeleteArticle(int feedId, string url)
        {
            DataProvider.DeleteArticle(feedId, url);
        }

        internal List<int> GetArticleVotes(int articleId)
        {
            return DataProvider.GetArticleVotes(articleId);
        }

        public Article GetArticleWithNotApprovedTags()
        {
            return DataProvider.GetArticleWithNotApprovedTags();
        }

        public void UpdateTagArticleCounts()
        {
            DataProvider.UpdateTagArticleCounts();
        }

        public List<Feed> GetFeedsNotPublic()
        {
            return DataProvider.GetFeedsNotPublic();
        }

        public List<Tag> GetTags(TagsRequest request)
        {
            return DataProvider.GetTags(request);
        }

        public void SetFeedsAsUpdated(IEnumerable<int> feedIds)
        {
            DataProvider.SetFeedsAsUpdated(feedIds);
        }

        public List<Article> GetTaggedArticles(int userId)
        {
            return DataProvider.GetTaggedArticles(userId);
        }

        public List<Feed> GetSubmitedFeeds(int userId)
        {
            return DataProvider.GetSubmitedFeeds(userId);
        }

        public List<UserTaggedArticle> GetTaggedArticlesStatuses(int userId)
        {
            return DataProvider.GetTaggedArticlesStatuses(userId);
        }

        public List<Article> GetIgnoredArticles(int userId)
        {
            return DataProvider.GetIgnoredArticles(userId);
        }

        internal List<Article> GetArticlesPublishedAfterForTag(DateTime dateTime, int tagId)
        {
            return DataProvider.GetArticlesPublishedAfterForTag(dateTime, tagId);
        }

        internal List<int> GetArticlesVisitedAndPublishedAfter(int userId, DateTime dateTime)
        {
            return DataProvider.GetVisitedArticlesPublishedAfter(userId, dateTime);
        }

        public List<Article> GetArticles(int feedId)
        {
            return DataProvider.GetArticles(feedId);
        }

        internal List<Article> GetArticles(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return new List<Article>();
            return DataProvider.GetArticles(ids);
        }
    }
}
