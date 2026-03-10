namespace Econyx.Infrastructure.AiServices;

using System.Collections.Concurrent;

internal sealed class AiResponseCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiry;

    public AiResponseCache(TimeSpan? defaultExpiry = null)
    {
        _defaultExpiry = defaultExpiry ?? TimeSpan.FromMinutes(30);
    }

    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            value = (T)entry.Value;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null) where T : notnull
    {
        var entry = new CacheEntry(value, DateTime.UtcNow + (expiry ?? _defaultExpiry));
        _cache[key] = entry;
    }

    public void Evict()
    {
        var now = DateTime.UtcNow;
        var expired = _cache.Where(kvp => kvp.Value.ExpiresAt <= now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
            _cache.TryRemove(key, out _);
    }

    private sealed record CacheEntry(object Value, DateTime ExpiresAt);
}
