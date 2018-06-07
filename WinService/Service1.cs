using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using WinService.Debugger;
using System.Threading.Tasks;
using Core.Parsers;
using Core.Services;
using System.Threading;
using Core.Utilities;
using System.Windows.Forms;
using Core.Data;
using System.Net;
using System.IO;
using Core.Models;
using System.Xml;
using Core.Caching;
using StackExchange.Redis;
using HtmlAgilityPack;

namespace WinService
{
    public partial class Service1 : ServiceBase, IDebuggableService
    {
        private readonly FeedService FeedService = new FeedService();
        private readonly LuceneSearch LuceneSearch = new LuceneSearch();
        private readonly UserService UserService = new UserService();
        private readonly ImportService ImportService = new ImportService();

        public Service1()
        {
            InitializeComponent();
        }

        public void Start(string[] args)
        {
            //ParseNewFeeds();
            ParsePublicFeeds();
            //ParseNonPublicFeeds();
            ////IndexArticles();
            //DeleteOldArticlesFromIndex();
            ////StartNewsletterSending();
            //ImportOPMLFiles();
        }

        protected override void OnStart(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(currentDomain_UnhandledException);

            ParseNewFeeds();
            ParsePublicFeeds();
            ParseNonPublicFeeds();
            IndexArticles();
            DeleteOldArticlesFromIndex();
            StartNewsletterSending();
            ImportOPMLFiles();
        }

        private void currentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Mail.SendMeAnEmail("Exception in WinService", "Terminating: " + e.IsTerminating + " " + e.ExceptionObject.ToString());
        }

        private void ImportOPMLFiles()
        {
            TryAction(TimeSpan.FromSeconds(10), () =>
            {
                var opmls = FeedService.GetOPMLFilesToParse();
                var opmlsGroupedBy3 = opmls.Select((o, i) => new { OPML = o, Index = i })
                                           .GroupBy(g => g.Index / 3)
                                           .Select(g => g.ToList())
                                           .ToList();

                foreach (var opml in opmls)
                {
                    FeedService.UpdateOPMLAsParsed(opml.Id);
                }

                foreach (var group in opmlsGroupedBy3)
                {
                    Task.Factory.StartNew(() =>
                    {
                        foreach (var g in group)
                        {
                            var opml = g.OPML;
                            try
                            {
                                var feedsByFolders = GetFeeds(opml);
                                if (feedsByFolders == null || feedsByFolders.Count == 0)
                                {
                                    Mail.SendMeAnEmail("OPML file empty?", opml.FileName);
                                    continue;
                                }
                                var user = UserService.GetUser(opml.UserId);
                                if (user == null) continue;
                                user.MyFeeds = UserService.GetUserFeeds(opml.UserId);
                                foreach (var feedsInFolder in feedsByFolders)
                                {
                                    var folder = feedsInFolder.Key;
                                    var feeds = feedsInFolder.Value;

                                    if (feeds == null) continue;
                                    foreach (var feed in feeds)
                                    {
                                        ImportService.ImportUrl(user, feed.SiteUrl, feed.Url, folder);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Mail.SendMeAnEmail("Error in parse opml", opml.Id.ToString() + " " + ex.ToString());
                            }
                        }
                    });
                }
            });
        }

        private Dictionary<string, List<Feed>> GetFeeds(OPML opml)
        {
            var result = new Dictionary<string, List<Feed>>();

            string fileLoc = System.Diagnostics.Debugger.IsAttached ?
                @"D:\Dropbox\Projects\RSSReader\Web\OPML" :
                @"C:\websites\rssheap\OPML";
            string xml = File.ReadAllText(Path.Combine(fileLoc, opml.FileName));

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(xml);

            var outlines = htmlDoc.DocumentNode.SelectNodes("//outline");
            if (outlines == null) return null;

            foreach (var outline in outlines)
            {
                var attr = outline.Attributes["xmlUrl"];
                if (attr == null)  //it is a folder
                {
                    var folderName = outline.Attributes["title"] != null ? outline.Attributes["title"].Value : string.Empty;
                    if (folderName.IsNullOrEmpty())
                        folderName = outline.Attributes["text"] != null ? outline.Attributes["text"].Value : string.Empty;

                    if (folderName.IsNullOrEmpty())
                        folderName = string.Empty;

                    //maybe it is in format outline and than outline
                    var innerOutlines = outline.SelectNodes("//outline");
                    if (outlines == null) continue;

                    foreach (var innerOutline in innerOutlines)
                    {
                        var url = innerOutline.Attributes.FirstOrDefault(a => !a.Value.IsNullOrEmpty() && a.Name.ToLower() == "xmlurl");
                        if (url != null)
                        {
                            var siteUrlAttr = innerOutline.Attributes.FirstOrDefault(a => !a.Value.IsNullOrEmpty() && a.Name.ToLower() == "htmlurl");
                            var siteUrl = siteUrlAttr != null ? siteUrlAttr.Value : string.Empty;

                            var feed = new Feed
                            {
                                Url = url.Value,
                                SiteUrl = siteUrl
                            };
                            if (result.ContainsKey(folderName))
                            {
                                var feeds = result[folderName];
                                if (feeds == null) feeds = new List<Feed>();
                                feeds.Add(feed);
                            }
                            else
                            {
                                result.Add(folderName, new List<Feed> { feed });
                            }
                        }
                    }
                }
                else
                {
                    string url = attr.Value;
                    var siteUrl = outline.Attributes["htmlUrl"] != null ? outline.Attributes["htmlUrl"].Value : string.Empty;

                    var feed = new Feed
                    {
                        Url = url,
                        SiteUrl = siteUrl
                    };
                    if (result.ContainsKey(""))
                    {
                        var feeds = result[""];
                        if (feeds == null) feeds = new List<Feed>();
                        feeds.Add(feed);
                    }
                    else
                    {
                        result.Add("", new List<Feed> { feed });
                    }
                }
            }

            return result;
        }

        private void StartNewsletterSending()
        {
            TryAction(TimeSpan.FromHours(6), () =>
            {
                var date = DateTime.Now.AddDays(-7);

                //TODO: Optimize this not to get all the customers
                var subscribedCstomers = UserService.GetUsersSubscribedToNewsletters()
                                                    .Where(c => !c.GetEmailAddress().IsNullOrEmpty() &&
                                                                c.Created < date)
                                                    .ToList();

                var groupedBy50 = subscribedCstomers.Select((c, i) => new { Customer = c, Index = i })
                                                    .GroupBy(g => g.Index / 50)
                                                    .Select(g => g.ToList())
                                                    .ToList();

                foreach (var group in groupedBy50)
                {
                    var customers = group.Select(g => g.Customer).ToList();

                    var datesSent = UserService.GetUserNewsletterDates(customers.Select(c => c.Id));

                    foreach (var cust in customers)
                    {
                        var url = "http://www.rssheap.com/mail/articles?guid=" + cust.GUID;
                        try
                        {
                            var custEmail = cust.GetEmailAddress();
                            if (custEmail.IsNullOrEmpty()) continue;
                            if (cust.Created > date) continue;
                            if (!custEmail.IsEmailAddress()) continue;

                            var lastSent = datesSent.Where(c => c.Item1 == cust.Id)
                                                    .OrderByDescending(d => d)
                                                    .FirstOrDefault();

                            if (lastSent != null && lastSent.Item2 >= date) continue;

                            var html = string.Empty;
                            using (var wc = new WebClient())
                            {
                                wc.Encoding = Encoding.UTF8;
                                wc.Headers["User-Agent"] = "www.rssheap.com";
                                html = wc.DownloadString(url);

                                if (wc.GetStatusCode() == HttpStatusCode.OK)
                                {
                                    if (Mail.SendEmail(custEmail, "rssheap - weekly newsletter " + DateTime.Now.ToString("D"), html))
                                    {
                                        UserService.InsertNewsletterSent(DateTime.Now, cust.Id, custEmail);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Mail.SendMeAnEmail("Error in sending weekly newsletter for customer", "id: " + cust.Id + "<br/>" + url + "<br/>" + ex.ToString());
                        }
                    }
                }
            });
        }

        private void DeleteOldArticlesFromIndex()
        {
            TryAction(TimeSpan.FromHours(6), () =>
            {
                LuceneSearch.DeleteNotIndexedIn(DateTime.Now.AddDays(-7));
            });
        }

        private void ParseNewFeeds()
        {
            TryAction(TimeSpan.FromSeconds(30), () =>
            {
                var allFeeds = FeedService.GetFeedsThatAreNotParsed();
                var groupedBy3 = allFeeds.Select((f, i) => new { Feed = f, Index = i })
                                         .GroupBy(g => g.Index / 4)
                                         .Select(g => g.ToList())
                                         .ToList();

                foreach (var group in groupedBy3)
                {
                    var feeds = group.Select(g => g.Feed).ToList();

                    Task.Factory.StartNew((args) =>
                    {
                        var taskFeeds = args as List<Feed>;

                        foreach (var feed in taskFeeds)
                        {
                            new FeedParser().Parse(feed);
                        }
                        FeedService.SetFeedsAsUpdated(taskFeeds.Select(f => f.Id));

                    }, feeds);
                }
            });
        }

        private void IndexArticles()
        {
            TryAction(TimeSpan.FromHours(1), () =>
            {
                var articles = FeedService.GetArticlesIndexedBefore(DateTime.Now.AddDays(-7), 100);

                var errors = string.Empty;
                var indexedIds = new List<int>();
                foreach (var article in articles)
                {
                    try
                    {
                        LuceneSearch.AddUpdateIndex(article);
                        indexedIds.Add(article.Id);
                    }
                    catch (Exception ex)
                    {
                        errors += "Error in indexing article " + article.Id + " error:" + ex.ToString() + "<br/><br/>";
                    }
                }
                if (!string.IsNullOrEmpty(errors))
                {
                    Mail.SendMeAnEmail("Indexing finished with errors ", errors);
                }
                if (indexedIds.Count > 0 && !System.Diagnostics.Debugger.IsAttached)
                {
                    FeedService.UpdateArticlesAsIndexed(indexedIds);
                }
            });
        }

        private void ParsePublicFeeds()
        {
            TryAction(TimeSpan.FromMinutes(30), () =>
            {
                var allFeeds = FeedService.GetFeedsUpdatedBefore(true, DateTime.Now.AddDays(-1), 1000);

                var noOfGroups = 50;
                var groups = allFeeds.Select((f, i) => new { Feed = f, Index = i })
                                     .GroupBy(g => g.Index / noOfGroups)
                                     .Select(g => g.ToList())
                                     .ToList();

                var tasks = new List<Task>();
                var processedFeeds = new List<int>();
                foreach (var group in groups)
                {
                    var feeds = group.ToList().Select(f => f.Feed).ToList();

                    var newTask = Task.Factory.StartNew((args) =>
                    {
                        var taskFeeds = args as List<Feed>;
                        foreach (var feed in taskFeeds)
                        {
                            new FeedParser().Parse(feed);
                            FeedService.SetFeedsAsUpdated(new List<int> { feed.Id });
                        }


                    }, feeds);

                    tasks.Add(newTask);
                }

                if (tasks.Count > 0)
                {
                    Task.Factory.ContinueWhenAll(tasks.ToArray(), (_) =>
                    {
                        FeedService.UpdateTagArticleCounts();
                    });
                }
            });
        }

        private void ParseNonPublicFeeds()
        {
            TryAction(TimeSpan.FromHours(6), () =>
            {
                var allFeeds = FeedService.GetFeedsUpdatedBefore(false, DateTime.Now.AddDays(-1), 1000);

                var noOfGroups = 50;
                var groups = allFeeds.Select((f, i) => new { Feed = f, Index = i })
                                     .GroupBy(g => g.Index / noOfGroups)
                                     .Select(g => g.ToList())
                                     .ToList();

                var tasks = new List<Task>();
                var processedFeeds = new List<int>();
                foreach (var group in groups)
                {
                    var feeds = group.ToList().Select(f => f.Feed).ToList();

                    var newTask = Task.Factory.StartNew((args) =>
                    {
                        var taskFeeds = args as List<Feed>;
                        foreach (var feed in taskFeeds)
                        {
                            new FeedParser().Parse(feed);
                            FeedService.SetFeedsAsUpdated(new List<int> { feed.Id });
                        }


                    }, feeds);

                    tasks.Add(newTask);
                }

                if (tasks.Count > 0)
                {
                    Task.Factory.ContinueWhenAll(tasks.ToArray(), (_) =>
                    {
                        FeedService.UpdateTagArticleCounts();
                    });
                }
            });
        }

        public void TryAction(TimeSpan toSleep, Action action)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Mail.SendMeAnEmail("Error in " + action.Method.Name, ex.ToString());
                }
                finally
                {
                    Thread.Sleep(toSleep);
                    action();
                }
            });
        }

        protected override void OnStop()
        {
            Mail.SendMeAnEmail("Windows service stoped", DateTime.Now.ToString());
        }

        protected override void OnShutdown()
        {
            Mail.SendMeAnEmail("Windows service shutdown", DateTime.Now.ToString());
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Continue()
        {
            throw new NotImplementedException();
        }

        public EventLog GetEventLog()
        {
            return EventLog;
        }
    }
}
