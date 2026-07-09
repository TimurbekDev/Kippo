namespace Kippo.Callbacks;

/// <summary>
/// Backing store for the callback-data vault. Telegram caps <c>callback_data</c> at 64 bytes;
/// the vault sidesteps that limit by persisting an arbitrarily large payload here and putting only
/// a short token on the button. Swap the default in-memory implementation for a distributed one
/// (Redis, database) to keep tokens valid across restarts and multiple instances.
/// </summary>
public interface ICallbackStore
{
    /// <summary>Persists <paramref name="data"/> and returns a short token that fits in a button.</summary>
    string Save(string data);

    /// <summary>Resolves a token previously returned by <see cref="Save"/>. Returns <c>false</c> if unknown or expired.</summary>
    bool TryLoad(string token, out string data);
}
