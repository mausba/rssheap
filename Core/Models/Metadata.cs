using Core.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Core.Models
{
    public abstract class Metadata
    {
        private DataProvider provider = new DataProvider();

        protected abstract MetaEntity GetMetaEntity();

        [XmlIgnore]
        private List<KeyValuePair<string, string>> MetadataValues { get; set; }

        public virtual void AddMetadata(string key, object value)
        {
            PopulateMetadata();
            var valueStr = value.GetType().IsValueType ? value.ToString() : value.ToXml();
            var existing = MetadataValues.Find(m => m.Key == key);
            if(!existing.Equals(new KeyValuePair<string, string>()))
            {
                MetadataValues.Remove(existing);
            }
            MetadataValues.Add(new KeyValuePair<string, string>(key, valueStr));
        }

        public virtual void DeleteMetadata(string key)
        {
            PopulateMetadata();
            var items = MetadataValues.Where(x => x.Key == key).ToList();

            foreach (var item in items)
            {
                MetadataValues.RemoveAt(MetadataValues.IndexOf(item));
            }
        }

        public virtual void DeleteMetadata(string key, string value)
        {
            PopulateMetadata();

            var kvp = new KeyValuePair<string, string>(key, value);
            var index = MetadataValues.IndexOf(kvp);

            if (index >= 0)
            {
                MetadataValues.RemoveAt(index);
            }
        }

        public virtual T GetMetadataValue<T>(string key)
        {
            PopulateMetadata();

            IEnumerable<T> values = GetMetadataValues<T>(key);

            if (!values.Any()) { return default(T); }

            return values.First();
        }

        public virtual IEnumerable<T> GetMetadataValues<T>(string key)
        {
            PopulateMetadata();

            var values = new List<T>();

            var items = MetadataValues.FindAll(x => x.Key == key);

            foreach (var item in items)
            {
                object value = item.Value;
                if(value is T)
                {
                    values.Add((T)value);
                }
                else
                {
                    try
                    {
                        values.Add((T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value));
                    }
                    catch
                    {
                        try
                        {
                            var strValue = value.ToString();
                            values.Add(strValue.FromXml<T>());
                        }
                        catch
                        {
                            values.Add(default(T));
                        }
                    }
                }
            }

            return values.AsEnumerable();
        }

        public virtual IEnumerable<string> GetMetadataKeys()
        {
            PopulateMetadata();
            return MetadataValues.Select(md => md.Key);
        }

        public virtual bool SaveMetadata()
        {
            var entity = GetMetaEntity();
            bool result = provider.SaveMetadata(entity, MetadataValues);
            MetadataValues = provider.GetAllMetadata(entity);

            return result;
        }

        protected virtual void PopulateMetadata()
        {
            if (MetadataValues == null)
            {
                MetadataValues = provider.GetAllMetadata(GetMetaEntity());
            }
        }
    }

    public class MetaEntity
    {
        public int EntityId { get; set; }
        public string EntityName { get; set; }
    }
}
