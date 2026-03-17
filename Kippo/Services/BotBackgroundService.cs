using Kippo.Handlers;
using Microsoft.Extensions.Hosting;

namespace Kippo.Services;

public class BotBackgroundService(ITelegramBotClient botClient, BotUpdateHandlerAdapter updateHandler) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery,
                UpdateType.EditedMessage,
                UpdateType.ChatMember,
                UpdateType.MyChatMember
            }
        };

        botClient.StartReceiving(
            updateHandler: updateHandler,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        return Task.CompletedTask;
    }
}
