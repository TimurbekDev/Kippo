using Kippo.Handlers;
using Kippo.Middleware;
using Kippo.Services;
using Kippo.SessionStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Kippo.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddKippo<THandler>(this IServiceCollection services,IConfiguration configuration) where THandler : class, IBotUpdateHandler
    {
        var botToken = configuration.GetSection("Kippo")["BotToken"]
            ?? throw new InvalidOperationException("Kippo:BotToken configuration is required.");

        AddBotClient(services, configuration);
        services.AddSingleton<ISessionStore,InMemorySessionStore>();
        
        services.AddSingleton<IBotUpdateHandler>(sp =>
        {
            var handler = ActivatorUtilities.CreateInstance<THandler>(sp);
            
            if (handler is BotUpdateHandler botHandler)
            {
                var sessionStore = sp.GetRequiredService<ISessionStore>();
                var middlewares = sp.GetServices<IBotMiddleware>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger(typeof(THandler));
                
                botHandler.Initialize(sessionStore, middlewares, logger, sp);
            }
            
            return handler;
        });
        
        services.AddSingleton<BotUpdateHandlerAdapter>();
        services.AddHostedService<BotBackgroundService>();
        return services;
    }

    private static void AddBotClient(IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(s => s.ServiceType == typeof(ITelegramBotClient)))
            return;

        var botToken = configuration.GetSection("Kippo")["BotToken"]
            ?? throw new InvalidOperationException("Kippo:BotToken configuration is required.");

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
        services.AddSingleton<TelegramBotClient>(sp => (TelegramBotClient)sp.GetRequiredService<ITelegramBotClient>());
    }
}
