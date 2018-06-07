using Core.Caching.Clients;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Core.Caching
{
    public interface ICacheClient
    {
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
        T Get<T>(string key);
        void Remove(string key);
        void Insert(object value, string key, CachePeriod cachePeriod);
        T GetOrAdd<T>(string key, CachePeriod cachePeriod, Func<T> getItemsFunc);
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void Clear(Collection<string> keys = null);
    }

    public static class CacheClient
    {
        public static ICacheClient Default
        {
            get
            {
                return new InMemoryCache();
                //HttpContext.Current.Request.Url.AbsoluteUri.Contains("localhost") ? (ICacheClient)new JsonFileCache() : new InMemoryCache();
            }
        }

        public static ICacheClient InMemoryCache
        {
            get { return new InMemoryCache(); }
        }

        public static ICacheClient XmlFileCache
        {
            get { return new JsonFileCache(); }
        }
    }
}
