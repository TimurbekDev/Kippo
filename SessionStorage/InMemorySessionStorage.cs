using System.Collections.Concurrent;

namespace Kippo.SessionStorage;

public class InMemorySessionStore : ISessionStore, IDisposable
{
    private readonly ConcurrentDictionary<long, Session> _storage = new();
    private readonly SessionOptions _options;
    private readonly Timer? _sweepTimer;

    public InMemorySessionStore(SessionOptions? options = null)
    {
        _options = options ?? new SessionOptions();

        if (_options.Ttl.HasValue)
        {
            _sweepTimer = new Timer(_ => Sweep(), null, _options.SweepInterval, _options.SweepInterval);
        }
    }

    public Task<Session> GetAsync(long chatId)
    {
        var session = _storage.GetOrAdd(chatId, _ => new Session());
        session.LastAccess = DateTimeOffset.UtcNow;
        EvictIfOverCapacity();
        return Task.FromResult(session);
    }

    public Task SaveAsync(long chatId, Session session)
    {
        session.LastAccess = DateTimeOffset.UtcNow;
        _storage[chatId] = session;
        session.ClearDirty();
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(long chatId)
    {
        var removed = _storage.TryRemove(chatId, out _);
        return Task.FromResult(removed);
    }

    private void Sweep()
    {
        if (_options.Ttl is not TimeSpan ttl)
            return;

        var cutoff = DateTimeOffset.UtcNow - ttl;
        foreach (var kvp in _storage)
        {
            if (kvp.Value.LastAccess < cutoff)
                _storage.TryRemove(kvp.Key, out _);
        }
    }

    private void EvictIfOverCapacity()
    {
        if (_options.MaxSessions is not int max || _storage.Count <= max)
            return;

        var toRemove = _storage.Count - max;
        foreach (var kvp in _storage.OrderBy(k => k.Value.LastAccess).Take(toRemove))
            _storage.TryRemove(kvp.Key, out _);
    }

    public void Dispose() => _sweepTimer?.Dispose();
}
