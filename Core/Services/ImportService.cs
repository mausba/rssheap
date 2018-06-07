using Core.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.Services
{
    public class ImportService
    {
        private FeedService FeedService = new FeedService();
        private UserService UserService = new UserService();

        public bool ImportUrl(User user, string htmlUrl, string rssUrl, string folderName = null)
        {
            if (!htmlUrl.IsNullOrEmpty() && !htmlUrl.ToLower().StartsWith("http") && !htmlUrl.ToLower().StartsWith("https") && !htmlUrl.ToLower().StartsWith("www"))
            {
                htmlUrl = "http://" + htmlUrl;
            }

            try
            {
                var urls = new List<string>();
                if (rssUrl.IsNullOrEmpty() && htmlUrl.IsNullOrEmpty()) return false;
                if (!htmlUrl.IsNullOrEmpty() && !htmlUrl.StartsWith("http://") && !htmlUrl.StartsWith("https")) htmlUrl = "http://" + rssUrl;

                Feed exsFeed = null;
                if (!htmlUrl.IsNullOrEmpty())
                    exsFeed = FeedService.GetFeedBySiteUrl(htmlUrl);

                if (exsFeed == null && !rssUrl.IsNullOrEmpty())
                    exsFeed = FeedService.GetFeedByXmlUrl(rssUrl);

                if (exsFeed != null)
                {
                    var userFeed = user.MyFeeds.Find(f => f.FeedId == exsFeed.Id);

                    if (userFeed == null)
                    {
                        userFeed = new UserFeed
                        {
                            FeedId = exsFeed.Id,
                            UserId = user.Id,
                            Subscribed = true,
                            Submited = true
                        };
                        userFeed.Id = UserService.InsertUserFeed(userFeed);
                        user.MyFeeds.Add(userFeed);
                        user.Reputation += 3;
                        UserService.UpdateUser(user);
                    }

                    if (!folderName.IsNullOrEmpty())
                    {
                        //create folder
                        var folder = UserService.GetUserFolder(user.Id, folderName);
                        if (folder == null)
                        {
                            folder = UserService.InsertUserFolder(user.Id, new Folder
                            {
                                Name = folderName,
                                UserId = user.Id
                            });
                            user.Folders.Add(folder);
                        }
                        if (folder != null)
                        {
                            try
                            {
                                UserService.InsertUserFeedFolder(user.Id, exsFeed.Id, folder.Id);
                            }
                            catch { }
                        }
                    }

                    return true;
                }

                try
                {
                    var wc = new WebClient
                    {
                        Encoding = Encoding.UTF8
                    };
                    wc.Headers["User-Agent"] = "www.rssheap.com";
                    var str = wc.DownloadString(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl).Replace("media:thumbnail", "media");  //mashable fix

                    var reader = XmlReader.Create(new StringReader(str));

                    if (reader.IsXmlFeed())
                    {
                        urls.Add(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl);
                    }
                    else
                    {
                        //it's not an xml, try to find it from the tag
                        var request = FeedService.CreateRequest(!rssUrl.IsNullOrEmpty() ? rssUrl : htmlUrl);
                        request.Timeout = 5000;
                        var response = (HttpWebResponse)request.GetResponse();
                        try
                        {
                            var html = new HtmlAgilityPack.HtmlDocument();
                            html.LoadHtml(new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd());
                            response.Close();

                            var links = html.DocumentNode
                                            .SelectNodes("//link")
                                            .Where(l => l.Attributes["rel"] != null && !l.Attributes["rel"].Value.IsNullOrEmpty() && l.Attributes["rel"].Value.ToLower() == "alternate" &&
                                                        l.Attributes["type"] != null && !l.Attributes["type"].Value.IsNullOrEmpty() && l.Attributes["type"].Value.Contains("xml") &&
                                                        l.Attributes["href"] != null && !l.Attributes["href"].Value.IsNullOrEmpty() && !l.Attributes["href"].Value.ToLower().Contains("comment"))
                                            .ToList();

                            if (links.Count > 1)
                            {
                                //try to find atom first
                                var temp = links.Where(l => l.Attributes["type"].Value.ToLower() == "application/atom+xml");
                                if (temp.Count() > 0)
                                {
                                    links = temp.ToList();
                                }
                                else
                                {
                                    temp = links.Where(l => l.Attributes["type"].Value.ToLower() == "application/rss+xml");
                                    if (temp.Count() > 0)
                                    {
                                        links = temp.ToList();
                                    }
                                    else
                                    {
                                        links = new List<HtmlNode> { links.First() };
                                    }
                                }
                            }

                            urls = links.Select(l => l.Attributes["href"].Value)
                                        .ToList();

                            if (urls.Count > 1) //usualy you get main feed and post feed, ignore the post feed
                            {
                                urls = urls.Where(u => !u.Contains(rssUrl)).ToList();
                            }

                        }
                        catch { }
                    }
                    if (urls.Count == 0) return false;

                    foreach (var u in urls)
                    {
                        var url = u;
                        if (url.StartsWith("/"))
                            url = new Uri(rssUrl).GetLeftPart(UriPartial.Authority) + url;

                        var rFeed = FeedService.GetRemoteFeed(new Feed
                        {
                            Url = url,
                            SiteUrl = htmlUrl
                        }, timeout: 3000);
                        if (rFeed != null)
                        {
                            if (!rFeed.Name.IsNullOrEmpty() && rFeed.Name.ToLower().Contains("comment")) continue;

                            var blogUrl = rFeed.SiteUrl;
                            if (blogUrl.IsNullOrEmpty())
                                blogUrl = rFeed.Url;

                            var lFeed = FeedService.GetFeedBySiteUrl(blogUrl);
                            int feedId = lFeed == null ? FeedService.InsertFeed(rFeed) : lFeed.Id;
                            var userFeed = user.MyFeeds.Find(f => f.FeedId == feedId);

                            if (userFeed == null)
                            {
                                userFeed = new UserFeed
                                {
                                    FeedId = feedId,
                                    UserId = user.Id,
                                    Subscribed = true,
                                    Submited = true
                                };
                                userFeed.Id = UserService.InsertUserFeed(userFeed);
                                user.MyFeeds.Add(userFeed);
                                user.Reputation += 3;
                                UserService.UpdateUser(user);
                            }

                            if (!folderName.IsNullOrEmpty())
                            {
                                //create folder
                                var folder = UserService.GetUserFolder(user.Id, folderName);
                                if (folder == null)
                                {
                                    folder = UserService.InsertUserFolder(user.Id, new Folder
                                    {
                                        Name = folderName,
                                        UserId = user.Id
                                    });
                                    user.Folders.Add(folder);
                                }
                                if (folder != null)
                                {
                                    try
                                    {
                                        UserService.InsertUserFeedFolder(user.Id, feedId, folder.Id);
                                    }
                                    catch { }
                                }
                            }
                            return true;
                        }
                    }
                }
                catch
                {

                }
            }
            catch
            {
            }
            return false;
        }
    }
}
