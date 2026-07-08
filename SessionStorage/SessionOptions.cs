namespace Kippo.SessionStorage;

public class SessionOptions
{
    /// <summary>Sliding expiration. Sessions untouched for longer than this are evicted. Null = never expire.</summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>Max number of live sessions. Oldest (least recently accessed) are evicted over the limit. Null = unbounded.</summary>
    public int? MaxSessions { get; set; }

    /// <summary>How often the background sweep runs to remove expired sessions. Only used when <see cref="Ttl"/> is set.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromMinutes(5);
}
