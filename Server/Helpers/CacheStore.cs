using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Server.Helpers
{
    public class CacheStore
    {
        private static MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        private const string KEY_LIBRARY_FILES = "Library Files";
        private const string KEY_SETTINGS = "Settings";

        public static List<LibraryFile> GetLibraryFiles()
        {
            return GetOrCreate(KEY_LIBRARY_FILES,
                () => DbHelper.Select<LibraryFile>().ToList(),
                absExpiration: 10
            );
        }

        public static Settings GetSettings()
        {
            return GetOrCreate(KEY_SETTINGS,
                () => DbHelper.Single<Settings>() ?? new Settings()
            );
        }

        public static void ClearLibraryFiles() => _cache.Remove(KEY_LIBRARY_FILES);
        public static void ClearSettings() => _cache.Remove(KEY_SETTINGS);

        public static void SetSettings(Settings settings)
        {
            GetOrCreate(KEY_SETTINGS, () => settings, 60, 120, true);
        }

        public static TItem GetOrCreate<TItem>(object key, Func<TItem> createItem, int slidingExpiration = 5, int absExpiration = 30, bool force = false)
        {
            TItem cacheEntry;
            if (force || _cache.TryGetValue(key, out cacheEntry) == false)// Look for cache key.
            {
                // Key not in cache, so get data.
                cacheEntry = createItem();

                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
                cacheEntryOptions.SetPriority(CacheItemPriority.High);
                if(slidingExpiration > 0) 
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration));
                if(absExpiration > 0)
                    cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromSeconds(absExpiration));

                // Save data in cache.
                _cache.Set(key, cacheEntry, cacheEntryOptions);
            }
            return cacheEntry;
        }
    }
}
