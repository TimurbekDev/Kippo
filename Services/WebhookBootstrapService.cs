using Kippo.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kippo.Services;

/// <summary>
/// Startup work for webhook mode (registered when <c>AddKippo(..., useLongPolling: false)</c>):
/// registers the bot's command menu, and — when <c>Kippo:WebhookUrl</c> is configured — tells
/// Telegram where to deliver updates via <c>SetWebhook</c>. Inbound updates themselves are handled
/// by <c>MapKippoWebhook</c>, not here.
/// </summary>
public class WebhookBootstrapService(
    ITelegramBotClient botClient,
    IBotUpdateHandler handler,
    IConfiguration configuration,
    ILogger<WebhookBootstrapService>? logger = null) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RegisterCommandsAsync(stoppingToken);
        await ConfigureWebhookAsync(stoppingToken);
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

    private async Task ConfigureWebhookAsync(CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Kippo");
        var url = section["WebhookUrl"];
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            await botClient.SetWebhook(
                url: url,
                secretToken: section["WebhookSecret"],
                cancellationToken: cancellationToken);

            logger?.LogInformation("Kippo: webhook registered at {Url}.", url);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Kippo: failed to set webhook to {Url}.", url);
        }
    }
}
