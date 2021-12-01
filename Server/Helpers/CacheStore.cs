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

        public static Task<List<LibraryFile>> GetLibraryFiles()
        {
            return GetOrCreate(KEY_LIBRARY_FILES,
                () => Task.FromResult(DbHelper.Select<LibraryFile>().ToList())
            );
        }

        public static void ClearLibraryFiles() => _cache.Remove("KEY_LIBRARY_FILES");

        public static async Task<T> GetOrCreate<T>(object key, Func<Task<T>> createItem)
        {
            T cacheEntry;

            if (!_cache.TryGetValue(key, out cacheEntry))// Look for cache key.
            {
                SemaphoreSlim mylock = _locks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));

                await mylock.WaitAsync();
                try
                {
                    if (!_cache.TryGetValue(key, out cacheEntry))
                    {
                        // Key not in cache, so get data.
                        cacheEntry = await createItem();
                        _cache.Set(key, cacheEntry);
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }
            return cacheEntry;
        }
    }
}
