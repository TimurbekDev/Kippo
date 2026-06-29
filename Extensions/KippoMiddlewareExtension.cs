using Kippo.Middleware;
using Microsoft.Extensions.DependencyInjection;


namespace Kippo.Extensions;

public static class KippoMiddlewareExtensions
{
    public static IServiceCollection AddKippoMiddleware<T>(
        this IServiceCollection services)
        where T : class, IBotMiddleware
    {
        services.AddSingleton<IBotMiddleware, T>();
        return services;
    }
}