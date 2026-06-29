namespace Kippo.Handlers;

public class BotUpdateHandlerAdapter : IUpdateHandler
{
    private readonly IBotUpdateHandler _handler;

    public BotUpdateHandlerAdapter(IBotUpdateHandler handler)
    {
        _handler = handler;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            await _handler.HandleUpdateAsync(botClient, update, cancellationToken);
        }
        catch (Exception ex)
        {
            await _handler.HandleErrorAsync(botClient, ex, cancellationToken);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        return _handler.HandleErrorAsync(botClient, exception, cancellationToken);
    }
}
