using Kippo.Contexs;

namespace Kippo.Middleware;

public class SessionMiddleware : IBotMiddleware
{
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
        
        context.Session = await context.SessionStore.GetAsync(chatId);

        await next();

        if (context.Session != null)
        {
            await context.SessionStore.SaveAsync(chatId, context.Session);
        }
    }
}
