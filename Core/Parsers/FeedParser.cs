using Core.Caching;
using Core.Models;
using Core.Services;
using Core.Utilities;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Core.Parsers
{
    public class FeedParser
    {
        private FeedService FeedService { get; set; }
        private LuceneSearch LuceneSearch { get; set; }
        private readonly object _updateFeedLock = new object();
        private readonly object _insertArticleLock = new object();

        public FeedParser()
        {
            FeedService = new FeedService();
            LuceneSearch = new LuceneSearch();
        }

        public void ParseAllFeeds()
        {
            var feeds = FeedService.GetFeeds().Where(f => f.Updated <= DateTime.Now.AddDays(-1));

            foreach (var feed in feeds)
            {
                Parse(feed);
            }
        }

        public void Parse(Feed lFeed)
        {
            try
            {
                lFeed.Updated = DateTime.Now;

                var rFeed = FeedService.GetRemoteFeed(lFeed);
                if (rFeed == null) return;

                if (rFeed.Name != lFeed.Name || rFeed.Description != lFeed.Description || rFeed.SiteUrl != lFeed.SiteUrl)
                {
                    lFeed.Name = rFeed.Name;
                    lFeed.Description = rFeed.Description;
                    lFeed.SiteUrl = rFeed.SiteUrl;
                    FeedService.UpdateFeed(lFeed);
                }

                lFeed.Articles = FeedService.GetArticlesWithoutBody(lFeed.Id);

                var lTags = CacheClient.Default.GetOrAdd("parsingLocalTags",
                    CachePeriod.ForMinutes(15),
                    () => FeedService.GetTagsWithArticlesCountGreaterThan(0));

                var uniqueArticles = rFeed.Articles.GroupBy(a => a.Url).Select(a => a.First()).ToList();
                foreach (var rArticle in uniqueArticles)
                {
                    try
                    {
                        if (!lFeed.Public &&
                           (
                                rArticle.Published > DateTime.MinValue &&
                                rArticle.Published <= DateTime.Now.AddYears(-1))
                           )
                            continue;

                        var lArticle = lFeed.Articles.Find(le => le.Url == rArticle.Url);
                        if (lArticle != null)
                        {
                            if (lArticle.Name != rArticle.Name)
                            {
                                lArticle.Name = rArticle.Name;
                                lArticle.Body = rArticle.Body;
                                FeedService.UpdateArticle(lArticle);

                                if (!Debugger.IsAttached)
                                {
                                    LuceneSearch.AddUpdateIndex(lArticle);
                                    Redis.UpdateArticleName(lArticle);
                                }
                            }
                            rFeed.Articles.Remove(rArticle);
                            continue;
                        }

                        rArticle.FeedId = lFeed.Id;
                        if (rFeed.Public && rArticle.Published <= DateTime.Now.AddDays(-32)) rArticle.Published = DateTime.Now;

                        if (rArticle.FeedId == 5569 && rArticle.Name.ToLower().Contains("comment on")) continue;

                        rArticle.Tags = GetArticleTags(rArticle, lTags, rFeed.Encoding);

                        if (lFeed.Public)
                            rArticle.LikesCount = Facebook.GetNumberOfLikes(rArticle.Url) +
                                                  Twitter.GetNumberOfTweets(rArticle.Url) +
                                                  LinkedIn.GetNumberOfShares(rArticle.Url) +
                                                  Google.GetNumberOfShares(rArticle.Url) +
                                                  Reddit.GetNumberOfVotes(rArticle.Url);

                        lock (_insertArticleLock)
                        {
                            rArticle.Id = FeedService.InsertArticle(rArticle);
                        }
                        rArticle.Feed = lFeed;

                        if (!Debugger.IsAttached)
                        {
                            LuceneSearch.AddUpdateIndex(rArticle);
                            Redis.AddArticle(rArticle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Mail.SendMeAnEmail("Error in insert article", "FeedId: " + lFeed.Id + " " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Mail.SendMeAnEmail("Error in parse feed", +lFeed.Id + " " + ex.ToString());
            }
        }

        private List<Tag> GetArticleTags(Article article, List<Tag> tags, Encoding encoding)
        {
            var name = article.Name.ToLower().Replace("'s", string.Empty);
            var body = article.Body;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(article.Url);
                request.Headers["Accept-Encoding"] = "gzip,deflate";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.KeepAlive = true;
                request.Timeout = 10000;
                request.MaximumAutomaticRedirections = 3;
                request.MaximumResponseHeadersLength = 1024 * 64 * 64;
                if (request != null)
                {
                    var response = request.GetResponse() as HttpWebResponse;

                    var xFrameOptions = string.Empty;

                    if (response.Headers != null)
                    {
                        xFrameOptions = response.Headers["X-Frame-Options"];
                    }
                    if (!xFrameOptions.IsNullOrEmpty())
                    {
                        if (xFrameOptions == "deny" ||
                            xFrameOptions == "SAMEORIGIN" ||
                            !xFrameOptions.Contains("rssheap"))
                        {
                            article.Flagged = true;
                        }
                    }


                    string contentType = "";
                    if (response != null)
                        contentType = response.ContentType;
                    if (contentType.Contains("html"))
                    {
                        using (Stream stream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(stream, encoding);
                            var responseString = reader.ReadToEnd();
                            if (!responseString.IsNullOrEmpty())
                                body = responseString;
                        }
                    }
                }
            }
            catch { }

            var keywords = new List<string>();
            var categories = new List<string>();
            if (!body.IsNullOrEmpty())
            {
                bool containsHTML = body.Contains("body");

                if (containsHTML)
                {
                    try
                    {
                        var html = new HtmlDocument();
                        html.LoadHtml(body);
                        html.DocumentNode
                            .Descendants()
                            .Where(d => d.Name == "style" || d.Name == "script")
                            .ToList()
                            .ForEach(n => n.Remove());

                        var text = html.DocumentNode.InnerText;

                        keywords = text.GetWordOccurences()
                                              .Where(w => w.Value >= 5)
                                              .Select(w => w.Key.ToLower())
                                              .ToList();

                        categories = html.DocumentNode.SelectNodes("//a")
                                             .Where(l => l.Attributes["href"] != null &&
                                                         !string.IsNullOrEmpty(l.Attributes["href"].Value) &&
                                                         l.Attributes["href"].Value.Contains("tags") &&
                                                         !string.IsNullOrEmpty(l.InnerText))
                                             .Select(l => l.InnerText.ToLower())
                                             .ToList();
                    }
                    catch (Exception ex)
                    {
                        Mail.SendMeAnEmail("Exception in GetArticleTags", article.FeedId + ex.ToString());
                    }
                }
            }

            var newTags = new List<Tag>();

            var nameWords = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.Replace(":", string.Empty)
                                              .Replace(";", string.Empty)
                                )
                                .ToList();
            foreach (var tag in tags)
            {
                tag.Approved = true;

                var tagNames = new List<string>
                {
                    tag.Name.Replace("-", " "),
                    tag.Name
                };

                bool found = false;
                if (nameWords.Any(nw => tagNames.Contains(nw)))
                {
                    found = true;
                    newTags.Add(tag);
                }

                if (!found)
                {
                    //if tag has more than 3 char let's try to match it
                    //because we don't want to match c tag to every title that contains c

                    if (!tagNames.Any(tn => tn.Length <= 3))
                    {
                        foreach (var tagName in tagNames)
                        {
                            if (name.Contains(" " + tagName + " "))
                                newTags.Add(tag);
                        }
                    }
                }

                if (!tag.MatchTitleOnly)
                {
                    if (keywords.Any(kw => tagNames.Contains(kw)))
                    {
                        newTags.Add(tag);
                    }
                }

                if (categories.Any(c => tagNames.Contains(c)))
                {
                    newTags.Clear();
                    newTags.Add(tag);
                }
            }

            name = null;
            body = null;
            return newTags.OrderByDescending(t => t.ArticlesCount).Distinct().ToList();
        }
    }
}
