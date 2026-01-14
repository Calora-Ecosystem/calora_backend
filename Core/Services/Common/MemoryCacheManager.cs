using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Services.Common;

public class MemoryCacheManager : IMemoryCache
{
    private readonly IMemoryCache _cache;
    private static readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheManager(IMemoryCache cache)
    {
        _cache = cache;
    }

    // wildcard/prefix delete
    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix))
            {
                this.Remove(key);
                _keys.TryRemove(key, out _);
            }
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
        _keys.Clear();
    }

    public ICacheEntry CreateEntry(object key)
    {
        _keys.TryAdd(key.ToString()!, 0);
        return _cache.CreateEntry(key);
    }

    public void Remove(object key)
    {
        _cache.Remove(key);
    }

    public bool TryGetValue(object key, out object? value)
    {
        return _cache.TryGetValue(key, out value);
    }
}