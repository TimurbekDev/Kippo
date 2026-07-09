using Kippo.Callbacks;
using Kippo.Contexs;

namespace Kippo.Middleware;

/// <summary>
/// Rehydrates vaulted callback buttons before routing. When a callback carries a <c>kv:</c> token,
/// this middleware resolves the stored envelope, rewrites the callback data to the original routing
/// key (so <c>[CallbackQuery]</c> patterns match as usual) and stashes the JSON payload for the
/// router to bind to a typed handler parameter. Non-vaulted updates pass through untouched.
/// </summary>
public sealed class CallbackVaultMiddleware : IBotMiddleware
{
    private readonly ICallbackStore _store;

    public CallbackVaultMiddleware(ICallbackStore store)
    {
        _store = store;
    }

    public async Task InvokeAsync(Context context, Func<Task> next)
    {
        var callback = context.Update.CallbackQuery;

        if (callback?.Data is string data && CallbackVault.IsVaultToken(data))
        {
            var token = CallbackVault.ExtractToken(data);
            if (_store.TryLoad(token, out var envelope) &&
                CallbackVault.TryUnpackEnvelope(envelope, out var route, out var payloadJson))
            {
                callback.Data = route;                                  // route as if the payload was inline
                context.Items[CallbackVault.PayloadItemKey] = payloadJson;
            }
        }

        await next();
    }
}
