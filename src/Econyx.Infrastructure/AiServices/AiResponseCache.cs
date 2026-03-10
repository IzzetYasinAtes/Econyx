namespace Econyx.Infrastructure.AiServices;

using System.Collections.Concurrent;

internal sealed class AiResponseCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiry;
    private readonly Timer _evictionTimer;

    public AiResponseCache(TimeSpan? defaultExpiry = null)
    {
        _defaultExpiry = defaultExpiry ?? TimeSpan.FromMinutes(30);
        _evictionTimer = new Timer(_ => Evict(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            if (entry.Value is T typed)
            {
                value = typed;
                return true;
            }
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null) where T : notnull
    {
        var entry = new CacheEntry(value, DateTime.UtcNow + (expiry ?? _defaultExpiry));
        _cache[key] = entry;
    }

    private void Evict()
    {
        var now = DateTime.UtcNow;
        var expired = _cache.Where(kvp => kvp.Value.ExpiresAt <= now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expired)
            _cache.TryRemove(key, out _);
    }

    public void Dispose() => _evictionTimer.Dispose();

    private sealed record CacheEntry(object Value, DateTime ExpiresAt);
}
