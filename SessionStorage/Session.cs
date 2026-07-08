using System.Collections.Concurrent;

namespace Kippo.SessionStorage;

public class Session
{
    private string? _state;

    public long UserId { get; set; }

    public string? State
    {
        get => _state;
        set
        {
            if (!string.Equals(_state, value, StringComparison.Ordinal))
            {
                _state = value;
                MarkDirty();
            }
        }
    }

    public ConcurrentDictionary<string, object> Data { get; set; } = new();

    /// <summary>True when the session has been mutated since it was loaded or last saved.</summary>
    internal bool IsDirty { get; private set; }

    /// <summary>Last time the session was read or written. Used for TTL/LRU eviction.</summary>
    internal DateTimeOffset LastAccess { get; set; } = DateTimeOffset.UtcNow;

    internal void MarkDirty() => IsDirty = true;

    internal void ClearDirty() => IsDirty = false;

    // --- State helpers (string) ---

    public void SetState(string? state) => State = state;

    public void ClearState() => State = null;

    public bool InState(string state) => string.Equals(State, state, StringComparison.Ordinal);

    // --- State helpers (enum, stored as its name) ---

    public void SetState<TEnum>(TEnum state) where TEnum : struct, Enum
        => State = state.ToString();

    public TEnum? GetState<TEnum>() where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(State, out var value) ? value : null;

    public bool InState<TEnum>(TEnum state) where TEnum : struct, Enum
        => string.Equals(State, state.ToString(), StringComparison.Ordinal);
}
