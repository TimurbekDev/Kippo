using Kippo.SessionStorage;

namespace Kippo.Extensions;

public static class SessionExtensions
{
    public static T? Get<T>(this Session session, string key)
        => session.Data.TryGetValue(key, out var value)
            ? (T)value
            : default;

    public static void Set<T>(this Session session, string key, T value)
        => session.Data[key] = value!;
}