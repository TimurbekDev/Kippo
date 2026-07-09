using System.Text.Json;
using Kippo.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kippo.Extensions;

/// <summary>
/// Maps a Telegram webhook endpoint that feeds incoming updates through the same Kippo router,
/// middleware and session pipeline as long polling. Use this instead of polling for production and
/// serverless deployments. Pair with <c>AddKippo(..., useLongPolling: false)</c>.
/// </summary>
public static class WebhookExtensions
{
    private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// Maps <c>POST {path}</c> as the bot's webhook receiver. When a secret token is configured
    /// (argument or <c>Kippo:WebhookSecret</c>), requests missing the matching
    /// <c>X-Telegram-Bot-Api-Secret-Token</c> header are rejected with 401.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder (e.g. the <c>WebApplication</c>).</param>
    /// <param name="path">The route to listen on. Default <c>/bot</c>.</param>
    /// <param name="secretToken">
    /// Shared secret Telegram echoes back on every request. Falls back to <c>Kippo:WebhookSecret</c>.
    /// </param>
    public static IEndpointConventionBuilder MapKippoWebhook(
        this IEndpointRouteBuilder endpoints,
        string path = "/bot",
        string? secretToken = null)
    {
        var configuration = endpoints.ServiceProvider.GetService<IConfiguration>();
        secretToken ??= configuration?.GetSection("Kippo")["WebhookSecret"];

        return endpoints.MapPost(path, async (HttpContext http) =>
        {
            if (!string.IsNullOrEmpty(secretToken))
            {
                var provided = http.Request.Headers[SecretHeader].ToString();
                if (!string.Equals(provided, secretToken, StringComparison.Ordinal))
                {
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            Update? update;
            try
            {
                update = await JsonSerializer.DeserializeAsync<Update>(
                    http.Request.Body, JsonBotAPI.Options, http.RequestAborted);
            }
            catch (JsonException)
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (update is null)
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var handler = http.RequestServices.GetRequiredService<IBotUpdateHandler>();
            var client = http.RequestServices.GetRequiredService<ITelegramBotClient>();
            await handler.HandleUpdateAsync(client, update, http.RequestAborted);

            http.Response.StatusCode = StatusCodes.Status200OK;
        });
    }
}
