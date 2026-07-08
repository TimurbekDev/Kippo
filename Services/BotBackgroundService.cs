using Kippo.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kippo.Services;

public class BotBackgroundService(
    ITelegramBotClient botClient,
    BotUpdateHandlerAdapter updateHandler,
    IBotUpdateHandler handler,
    ILogger<BotBackgroundService>? logger = null) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RegisterCommandsAsync(stoppingToken);

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
    }

    private async Task RegisterCommandsAsync(CancellationToken cancellationToken)
    {
        if (handler is not BotUpdateHandler botHandler)
            return;

        var commands = botHandler.BotCommands;
        if (commands.Count == 0)
            return;

        if (commands.Count > 100)
        {
            logger?.LogWarning(
                "Kippo: {Count} commands declared but Telegram allows a maximum of 100. Extra commands will be ignored.",
                commands.Count);
            commands = commands.Take(100).ToArray();
        }

        try
        {
            await botClient.SetMyCommands(commands, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Kippo: failed to register bot commands via SetMyCommands.");
        }
    }
}
