using Kippo.Contexs;
using Kippo.Middleware;
using Kippo.Routers;
using Kippo.SessionStorage;
using Microsoft.Extensions.Logging;

namespace Kippo.Handlers;

public abstract class BotUpdateHandler : IBotUpdateHandler
{
    private CommandRouter? _commandRouter;
    private ISessionStore? _sessionStore;
    private IServiceProvider? _serviceProvider;
    protected ILogger? Logger { get; private set; }

    protected BotUpdateHandler()
    {
    }

    protected BotUpdateHandler(
        ISessionStore sessionStore,
        IEnumerable<IBotMiddleware> middlewares,
        ILogger? logger = null,
        IServiceProvider? serviceProvider = null)
    {
        Initialize(sessionStore, middlewares, logger, serviceProvider);
    }

    internal void Initialize(
        ISessionStore sessionStore,
        IEnumerable<IBotMiddleware> middlewares,
        ILogger? logger = null,
        IServiceProvider? serviceProvider = null)
    {
        _sessionStore = sessionStore;
        _serviceProvider = serviceProvider;
        Logger = logger;
        _commandRouter = new CommandRouter(this, logger);

        foreach (IBotMiddleware middleware in middlewares)
        {
            _commandRouter.Use(middleware);
        }
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (_commandRouter == null || _sessionStore == null)
            throw new InvalidOperationException("Handler not initialized. This should not happen if registered via AddKippo.");

        var context = new Context(botClient, update, cancellationToken, _sessionStore, _serviceProvider);
        await _commandRouter.RouteAsync(context);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (Logger != null)
        {
            Logger.LogError(exception,
                "Unhandled exception in bot update handler: {ExceptionMessage}",
                exception.Message);
        }
        else
        {
            Console.Error.WriteLine($"[Kippo Error] {exception}");
        }

        return Task.CompletedTask;
    }
}