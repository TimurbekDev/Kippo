using System.Collections.Concurrent;

namespace Kippo.SessionStorage;

public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<long, Session> _storage = new();

    public Task<Session> GetAsync(long chatId)
    {
        var session = _storage.GetOrAdd(chatId, _ => new Session());
        return Task.FromResult(session);
    }

    public Task SaveAsync(long chatId, Session session)
    {
        _storage.AddOrUpdate(chatId, session, (_, _) => session);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(long chatId)
    {
        var removed = _storage.TryRemove(chatId, out _);
        return Task.FromResult(removed);
    }
}
