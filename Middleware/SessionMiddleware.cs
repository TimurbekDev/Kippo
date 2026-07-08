using System.Collections.Concurrent;
using Kippo.Contexs;

namespace Kippo.Middleware;

public class SessionMiddleware : IBotMiddleware
{
    // Fixed set of stripes: bounds memory (no per-chat semaphore leak) while still
    // serializing concurrent updates for the same chat. Distinct chats may share a
    // stripe (minor false contention) but never race on the same session.
    private const int LockStripes = 64;

    private static readonly SemaphoreSlim[] _locks =
        Enumerable.Range(0, LockStripes)
            .Select(_ => new SemaphoreSlim(1, 1))
            .ToArray();

    private static SemaphoreSlim GetLock(long chatId)
        => _locks[(int)((ulong)chatId % LockStripes)];

    public async Task InvokeAsync(Context context, Func<Task> next)
    {
        long chatId;
        try
        {
            chatId = context.ChatId;
        }
        catch (InvalidOperationException)
        {
            await next();
            return;
        }

        var gate = GetLock(chatId);
        await gate.WaitAsync(context.CancellationToken);
        try
        {
            context.Session = await context.SessionStore.GetAsync(chatId);

            await next();

            if (context.Session is { IsDirty: true })
            {
                await context.SessionStore.SaveAsync(chatId, context.Session);
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
