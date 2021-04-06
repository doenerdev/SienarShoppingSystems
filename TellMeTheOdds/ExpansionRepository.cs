using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;

namespace SienarShoppingSystems
{
    public class ExpansionRepository
    {
        private const string ExpansionCacheKey = "EXPANSIONS";
        private static ObjectCache expansionsCache = MemoryCache.Default;

        private IEnumerable<Expansion> Expansions { get
            {
                if (!expansionsCache.Contains(ExpansionCacheKey))
                    PopulateCache();
                return (IEnumerable<Expansion>)expansionsCache.Get(ExpansionCacheKey);
            } 
        }

        public IEnumerable<Expansion> GetAll()
        {
            return Expansions;
        }

        private void PopulateCache()
        {
            var expansions = new List<Expansion>();
            using (StreamReader r = new StreamReader("expansions.json"))
            {
                string json = r.ReadToEnd();
                expansions = JsonConvert.DeserializeObject<List<Expansion>>(json);
            }

            expansionsCache.Add(ExpansionCacheKey, expansions, new CacheItemPolicy());
        }
    }

    public class Expansion
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public IEnumerable<Content> Contents { get; set; }
    }

    public class Content
    {
        public string Name { get; set; }
        public ContentType Type { get; set; }
        public int Count { get; set; }
    }


    public enum ContentType
    {
        Ship,
        Pilot,
        Upgrade
    }
}
