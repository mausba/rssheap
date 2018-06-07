using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Core.Caching.Clients
{
    public class InMemoryCache : ICacheClient
    {
        private static MemoryCache Cache = MemoryCache.Default;

        public T Get<T>(string key)
        {
            return (T)Cache.Get(key);
        }

        public void Remove(string key)
        {
            Cache.Remove(key);
        }

        public T GetOrAdd<T>(string key, CachePeriod cachePeriod, Func<T> getItemsFunc)
        {
            if (cachePeriod == null) return getItemsFunc();

            var items = (T)Cache.Get(key);
            if (IsDefault(items))
            {
                items = getItemsFunc();
                if (!IsDefault(items))
                {
                    Insert(items, key, cachePeriod);
                }
            }
            return items;
        }

        public void Insert(object value, string key, CachePeriod cachePeriod)
        {
            Cache.Add(new CacheItem(key, value), new CacheItemPolicy
            {
                AbsoluteExpiration = new DateTimeOffset(cachePeriod.ToExpirationDate())
            });
        }

        public void Clear(Collection<string> keys = null)
        {
            foreach (var key in Cache.Select(k => k.Key))
            {
                Cache.Remove(key);
            }
        }

        private static bool IsDefault<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }
    }
}

