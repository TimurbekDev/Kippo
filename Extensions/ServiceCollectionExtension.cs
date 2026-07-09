using Kippo.Callbacks;
using Kippo.Client;
using Kippo.Handlers;
using Kippo.Middleware;
using Kippo.Services;
using Kippo.SessionStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;


namespace Kippo.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddKippo<THandler>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SessionOptions>? configureSession = null,
        Action<FloodControlOptions>? configureFloodControl = null) where THandler : class, IBotUpdateHandler
    {
        var botToken = configuration.GetSection("Kippo")["BotToken"]
            ?? throw new InvalidOperationException("Kippo:BotToken configuration is required.");

        FloodControlOptions? floodOptions = null;
        if (configureFloodControl != null)
        {
            floodOptions = new FloodControlOptions();
            configureFloodControl(floodOptions);
        }

        AddBotClient(services, configuration, floodOptions);

        var sessionOptions = new SessionOptions();
        configureSession?.Invoke(sessionOptions);
        services.AddSingleton(sessionOptions);
        services.AddSingleton<ISessionStore>(sp => new InMemorySessionStore(sp.GetRequiredService<SessionOptions>()));

        // Callback-data vault: lets inline buttons carry payloads larger than Telegram's 64-byte limit.
        services.TryAddSingleton<ICallbackStore>(_ => new InMemoryCallbackStore());
        services.AddSingleton<IBotMiddleware, CallbackVaultMiddleware>();
        
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

    private static void AddBotClient(IServiceCollection services, IConfiguration configuration, FloodControlOptions? floodOptions)
    {
        if (services.Any(s => s.ServiceType == typeof(ITelegramBotClient)))
            return;

        var botToken = configuration.GetSection("Kippo")["BotToken"]
            ?? throw new InvalidOperationException("Kippo:BotToken configuration is required.");

        var rawClient = new TelegramBotClient(botToken);
        services.AddSingleton<TelegramBotClient>(rawClient);

        if (floodOptions != null)
            services.AddSingleton<ITelegramBotClient>(new ThrottlingBotClient(rawClient, floodOptions));
        else
            services.AddSingleton<ITelegramBotClient>(rawClient);
    }
}
