using System.Collections.Concurrent;

namespace Kippo.SessionStorage;

public class Session
{
    public long UserId { get; set; }
    public string? State { get; set; }
    public ConcurrentDictionary<string, object> Data { get; set; } = new();
}
