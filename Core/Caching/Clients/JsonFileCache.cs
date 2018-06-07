using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.ObjectModel;

namespace Core.Caching.Clients
{
    public class JsonFileCache : ICacheClient
    {
        private const string CacheDirectory = "c://cache//";
        private const string FileExtension = ".json";
        private const int ChunkSize = 200;

        private static readonly object objLock = new object();

        public T Get<T>(string key)
        {
            return GetFromFile<T>(key);
        }

        public void Remove(string key)
        {
            Directory.GetFiles(CacheDirectory + "//", key + "*").ToList().ForEach(f => File.Delete(f));
        }

        public T GetOrAdd<T>(string key, CachePeriod cachePeriod, Func<T> GetItemsFunc)
        {
            lock (objLock)
            {
                var items = GetFromFile<T>(key);
                if (IsDefault(items))
                {
                    items = GetItemsFunc();
                    if (!IsDefault(items))
                    {
                        Insert(items, key, cachePeriod);
                    }
                }
                return items;
            }
        }

        public void Insert(object value, string key, CachePeriod cachePeriod)
        {
            try
            {
                string fileName = CacheDirectory + "//" +
                                  key +
                                  cachePeriod.GetKey() +
                                  " " +
                                  cachePeriod.ToExpirationDateString();

                if (value is IList list && list.Count > ChunkSize)
                {
                    var tempList = new List<object>();
                    foreach (var t in list)
                    {
                        tempList.Add(t);
                    }

                    var counter = 0;
                    while (tempList.Count > 0)
                    {
                        counter++;
                        var chunk = new List<object>();

                        for (int i = 0; i < tempList.Count; i++)
                        {
                            if (i >= ChunkSize)
                                break;

                            var item = tempList[i];
                            chunk.Add(item);
                        }

                        foreach (var item in chunk)
                        {
                            tempList.Remove(item);
                        }


                        File.WriteAllText(fileName + FileExtension + counter, chunk.ToJson());
                    }
                }
                else
                {
                    File.WriteAllText(fileName + FileExtension, value.ToJson());
                }
            }
            catch { }
        }

        public void Clear(Collection<string> keys = null)
        {
            Directory.GetFiles(CacheDirectory)
                     .ToList()
                     .ForEach(f => File.Delete(f));
        }

        private static bool IsDefault<T>(T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }

        private static T GetFromFile<T>(string key)
        {
            try
            {
                if (!Directory.Exists(CacheDirectory))
                    Directory.CreateDirectory(CacheDirectory);

                string pattern = key + "*" + FileExtension + "*";
                var fileNames = new DirectoryInfo(CacheDirectory).GetFileSystemInfos(pattern)
                                                                .OrderByDescending(f => f.CreationTime)
                                                                .Select(f => f.FullName)
                                                                .ToList();

                if (fileNames.Count == 0) return default(T);

                foreach (var fileName in fileNames.ToList())
                {
                    var expirationDate =
                        DateTime.ParseExact(Path.GetFileNameWithoutExtension(fileName).Split(' ')[1],
                                                                "(dd,MM,yyyy,HH,mm,ss)",
                                                                 CultureInfo.CurrentCulture);
                    if (DateTime.Now > expirationDate)
                    {
                        RetryFor3Times(() => { File.Delete(fileName); });
                        fileNames.Remove(fileName);
                    }
                }

                //now we are left with only valid files
                if (fileNames.Count == 0) return default(T);

                if (fileNames.Count > 1)
                {
                    //we have chunks of data, List
                    var result = Activator.CreateInstance<T>();
                    foreach (var fileName in fileNames)
                    {
                        var str = File.ReadAllText(fileName);
                        var obj = JsonConvert.DeserializeObject<T>(str);

                        foreach (var item in obj as IList)
                        {
                            ((IList)result).Add(item);
                        }
                    }
                    return result;
                }
                else
                {
                    var str = File.ReadAllText(fileNames.First());
                    return JsonConvert.DeserializeObject<T>(str);
                }
            }
            catch
            {
                return default(T);
            }
        }

        private static void RetryFor3Times(Action action, int times = 0)
        {
            try { action(); }
            catch
            {
                if (times <= 3)
                {
                    Thread.Sleep(2000);
                    RetryFor3Times(action, times++);
                }
            }
        }
    }
}