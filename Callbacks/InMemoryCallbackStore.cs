using System.Collections.Concurrent;

namespace Kippo.Callbacks;

/// <summary>
/// Default in-process <see cref="ICallbackStore"/>. Tokens survive as long as the process does,
/// bounded by a sliding TTL and a maximum entry count (oldest entries evicted first). Adequate for
/// single-instance bots; use a distributed store for scaled or restart-tolerant deployments.
/// </summary>
public sealed class InMemoryCallbackStore : ICallbackStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly TimeSpan _ttl;
    private readonly int _maxEntries;

    public InMemoryCallbackStore(TimeSpan? ttl = null, int maxEntries = 100_000)
    {
        _ttl = ttl ?? TimeSpan.FromHours(24);
        _maxEntries = maxEntries;
    }

    public string Save(string data)
    {
        var token = NewToken();
        _entries[token] = new Entry(data, DateTimeOffset.UtcNow);
        EvictIfNeeded();
        return token;
    }

    public bool TryLoad(string token, out string data)
    {
        if (_entries.TryGetValue(token, out var entry) && DateTimeOffset.UtcNow - entry.CreatedAt <= _ttl)
        {
            data = entry.Data;
            return true;
        }

        data = string.Empty;
        return false;
    }

    private void EvictIfNeeded()
    {
        if (_entries.Count <= _maxEntries)
            return;

        var cutoff = DateTimeOffset.UtcNow - _ttl;
        foreach (var kvp in _entries)
        {
            if (kvp.Value.CreatedAt < cutoff)
                _entries.TryRemove(kvp.Key, out _);
        }

        // Still over capacity after dropping expired entries: drop the oldest.
        var overflow = _entries.Count - _maxEntries;
        if (overflow > 0)
        {
            foreach (var kvp in _entries.OrderBy(k => k.Value.CreatedAt).Take(overflow))
                _entries.TryRemove(kvp.Key, out _);
        }
    }

    private static string NewToken()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private readonly record struct Entry(string Data, DateTimeOffset CreatedAt);
}
