using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Core.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Core.Utilities;
using Lucene.Net.Search.Similar;
using System.Globalization;
using System.Diagnostics;

namespace Core.Services
{
    public class LuceneSearch
    {
        private static readonly object _lock = new object();
        private readonly string _luceneDir = string.Empty;

        public LuceneSearch()
        {
            if (Debugger.IsAttached)
                _luceneDir = @"C:\Projects\rssheap\trunk\Web\lucene_index";
            else
                _luceneDir = @"C:\websites\rssheap\lucene_index";
        }

        private FSDirectory _directoryTemp;
        private FSDirectory _directory
        {
            get
            {
                if (_directoryTemp == null) _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp))
                    IndexWriter.Unlock(_directoryTemp);
                //var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                //if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }

        public IEnumerable<Article> Search(string input, int page, int pageSize, string fieldName = "")
        {
            if (string.IsNullOrEmpty(input)) return new List<Article>();

            var terms = input.Trim().Replace("-", " ").Split(' ')
                .Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            input = string.Join(" ", terms);

            return _search(input, page, pageSize, fieldName);
        }

        public List<Article> GetRelatedArticles(int articleId, int count)
        {
            var reader = IndexReader.Open(_directory, true);
            var searcher = new IndexSearcher(_directory, true);

            var searchQuery = new TermQuery(new Term("Id", articleId.ToString()));
            var doc = searcher.Search(searchQuery, 1);
            if (doc.TotalHits == 0) return new List<Article>();

            var docId = doc.ScoreDocs[0].Doc;

            MoreLikeThis mlt = new MoreLikeThis(reader);
            mlt.SetFieldNames(new[] { "Name", "Body", "TagName", "FeedName" });
            Query query = mlt.Like(docId);
            var hits = searcher.Search(query, count + 1);

            var articles = ConvertToArticles(hits, searcher, 1, count).Where(a => a.Id != articleId);

            reader.Dispose();
            searcher.Dispose();
            return articles.ToList();
        }

        public List<Article> SearchDefault(string input, int page, int pageSize, string fieldName = "")
        {
            return string.IsNullOrEmpty(input) ? new List<Article>() : _search(input, page, pageSize, fieldName).ToList();
        }

        public void AddUpdateIndex(Article article)
        {
            AddUpdateIndex(new List<Article> { article });
        }

        public void AddUpdateIndex(IEnumerable<Article> articles)
        {
            try
            {
                var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

                lock (_lock)
                {
                    using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        writer.SetMergeScheduler(new SerialMergeScheduler());

                        foreach (var article in articles)
                        {
                            if (article.Feed == null || !article.Feed.Public) continue;

                            var searchQuery = new TermQuery(new Term("Id", article.Id.ToString()));
                            writer.DeleteDocuments(searchQuery);

                            var doc = new Document();

                            doc.Add(new Field("Id", article.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("Name", article.Name, Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("Body", article.Body, Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("ViewsCount", article.ViewsCount.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("LikesCount", article.LikesCount.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("Published", article.Published.ToString("dd-MM-yyyy"), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("Indexed", DateTools.DateToString(DateTime.Now, DateTools.Resolution.SECOND), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("ShortUrl", article.ShortUrl, Field.Store.YES, Field.Index.NOT_ANALYZED));
                            doc.Add(new Field("FeedId", article.Feed.Id.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("FeedName", article.Feed.Name.ToString(), Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("FeedSiteUrl", article.Feed.SiteUrl, Field.Store.YES, Field.Index.NOT_ANALYZED));

                            var tagIdNames = string.Empty;
                            foreach (var tag in article.Tags)
                            {
                                tagIdNames += tag.Id + ";" + tag.Name + ",";
                                doc.Add(new Field("TagName", tag.Name, Field.Store.YES, Field.Index.ANALYZED));
                            }
                            tagIdNames = !tagIdNames.IsNullOrEmpty() ? tagIdNames.Substring(0, tagIdNames.Length - 1) : tagIdNames;

                            doc.Add(new Field("TagIdName", tagIdNames, Field.Store.YES, Field.Index.NOT_ANALYZED));

                            writer.AddDocument(doc);
                        }

                        analyzer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Mail.SendMeAnEmail("Error in update Index", ex.ToString());
            }
        }

        public void DeleteNotIndexedIn(DateTime date)
        {
            if ((DateTime.Now - date).TotalDays < 4) throw new Exception("Date must be at least 4 days");

            var reader = IndexReader.Open(_directory, true);
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                if (reader.IsDeleted(i))
                    continue;

                var doc = reader.Document(i);
                var docIndexedDate = !doc.Get("Indexed").IsNullOrEmpty() ? DateTools.StringToDate(doc.Get("Indexed")) : DateTime.MinValue;

                if (docIndexedDate < date)
                {
                    int articleId = int.TryParse(doc.Get("Id"), out articleId) ? articleId : 0;
                    if (articleId > 0)
                        ClearLuceneIndexRecord(articleId);
                }
            }
        }

        public void ClearLuceneIndexRecord(int articleId)
        {
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            lock (_lock)
            {
                using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.SetMergeScheduler(new SerialMergeScheduler());

                    var searchQuery = new TermQuery(new Term("Id", articleId.ToString()));
                    writer.DeleteDocuments(searchQuery);

                    analyzer.Close();
                }
            }
        }

        public bool ClearLuceneIndex()
        {
            try
            {
                var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                lock (_lock)
                {
                    using (var writer = new IndexWriter(_directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        writer.SetMergeScheduler(new SerialMergeScheduler());

                        writer.DeleteAll();

                        analyzer.Close();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void Optimize()
        {
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            lock (_lock)
            {
                using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.SetMergeScheduler(new SerialMergeScheduler());

                    analyzer.Close();
                    writer.Optimize();
                }
            }
        }

        private IEnumerable<Article> ConvertToArticles(TopDocs hits, IndexSearcher searcher, int page, int pageSize)
        {
            var result = new List<Article>();
            int first = (page - 1) * pageSize, last = ((page - 1) * pageSize) + pageSize;
            for (int i = first; i <= last && i < hits.ScoreDocs.Length; i++)
            {
                result.Add(ConvertToArticle(searcher.Doc(hits.ScoreDocs[i].Doc)));
            }
            return result;
        }

        private Article ConvertToArticle(Document doc)
        {
            var article = new Article
            {
                Id = Convert.ToInt32(doc.Get("Id")),
                Name = doc.Get("Name"),
                Body = doc.Get("Body"),
                ViewsCount = Convert.ToInt32(doc.Get("ViewsCount")),
                LikesCount = Convert.ToInt32(doc.Get("LikesCount")),
                Published = DateTime.ParseExact(doc.Get("Published"), "dd-MM-yyyy", CultureInfo.InvariantCulture),
                ShortUrl = doc.Get("ShortUrl"),
                Feed = new Feed
                {
                    Id = Convert.ToInt32(doc.Get("FeedId")),
                    Name = doc.Get("FeedName"),
                    SiteUrl = doc.Get("FeedSiteUrl")
                }
            };

            if (!doc.Get("Indexed").IsNullOrEmpty())
            {
                article.Indexed = DateTools.StringToDate(doc.Get("Indexed"));
            }

            var tagIdNames = doc.Get("TagIdName").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tagIdNames.Count(); i++)
            {
                article.Tags.Add(new Tag
                {
                    Id = Convert.ToInt32(tagIdNames[i].Split(';')[0]),
                    Name = tagIdNames[i].Split(';')[1]
                });
            }
            return article;
        }

        private Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private IEnumerable<Article> _search(string searchQuery, int page, int pageSize, string searchField = "")
        {
            page = page <= 0 ? 1 : page;
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<Article>();

            using (var searcher = new IndexSearcher(_directory, true))
            {
                var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

                if (!string.IsNullOrEmpty(searchField))
                {
                    var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, searchField, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search(query, page * pageSize);
                    var results = ConvertToArticles(hits, searcher, page, pageSize);
                    analyzer.Close();
                    return results;
                }
                else
                {
                    var parser = new MultiFieldQueryParser
                        (Lucene.Net.Util.Version.LUCENE_30, new[] { "Id", "Name", "Body", "TagName", "FeedName" }, analyzer);
                    var query = ParseQuery(searchQuery, parser);
                    var hits = searcher.Search
                    (query, null, page * pageSize, Sort.RELEVANCE);
                    var results = ConvertToArticles(hits, searcher, page, pageSize);
                    analyzer.Close();
                    return results;
                }
            }
        }
    }
}
