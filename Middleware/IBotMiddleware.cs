using Kippo.Contexs;

namespace Kippo.Middleware;

public interface IBotMiddleware
{
    Task InvokeAsync(Context context, Func<Task> next);
}
