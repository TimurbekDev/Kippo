using System.Collections.Concurrent;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;

namespace Kippo.Client;

/// <summary>
/// An <see cref="ITelegramBotClient"/> decorator that transparently survives Telegram flood limits.
/// It retries requests that fail with <c>429 Too Many Requests</c> (honoring the server-supplied
/// <c>retry_after</c>) and optionally throttles outbound traffic per chat. All other behavior is
/// forwarded unchanged to the wrapped client.
/// </summary>
public sealed class ThrottlingBotClient : ITelegramBotClient
{
    private readonly ITelegramBotClient _inner;
    private readonly FloodControlOptions _options;

    // Per-chat gate: serializes sends to the same chat and remembers the last send time so we can
    // enforce MinIntervalPerChat without a per-chat timer.
    private readonly ConcurrentDictionary<long, ChatGate> _chatGates = new();

    public ThrottlingBotClient(ITelegramBotClient inner, FloodControlOptions options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<TResponse> SendRequest<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var chatId = TryGetChatId(request);

        if (chatId is long id && _options.MinIntervalPerChat > TimeSpan.Zero)
        {
            var gate = _chatGates.GetOrAdd(id, _ => new ChatGate());
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WaitForChatSlot(gate, cancellationToken).ConfigureAwait(false);
                var response = await SendWithRetry(request, cancellationToken).ConfigureAwait(false);
                gate.LastSend = DateTimeOffset.UtcNow;
                return response;
            }
            finally
            {
                gate.Semaphore.Release();
            }
        }

        return await SendWithRetry(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResponse> SendWithRetry<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await _inner.SendRequest(request, cancellationToken).ConfigureAwait(false);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429 && attempt < _options.MaxRetries)
            {
                var retryAfter = ex.Parameters?.RetryAfter is int seconds
                    ? TimeSpan.FromSeconds(seconds)
                    : TimeSpan.FromSeconds(1);

                if (retryAfter > _options.MaxRetryAfter)
                    throw;

                attempt++;
                await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task WaitForChatSlot(ChatGate gate, CancellationToken cancellationToken)
    {
        var since = DateTimeOffset.UtcNow - gate.LastSend;
        var remaining = _options.MinIntervalPerChat - since;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
    }

    private static long? TryGetChatId(object request)
        => request is IChatTargetable { ChatId.Identifier: long id } ? id : null;

    private sealed class ChatGate
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public DateTimeOffset LastSend = DateTimeOffset.MinValue;
    }

    // --- Everything below is a straight pass-through to the wrapped client ---

    public bool LocalBotServer => _inner.LocalBotServer;
    public long BotId => _inner.BotId;

    public TimeSpan Timeout
    {
        get => _inner.Timeout;
        set => _inner.Timeout = value;
    }

    public IExceptionParser ExceptionsParser
    {
        get => _inner.ExceptionsParser;
        set => _inner.ExceptionsParser = value;
    }

    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest
    {
        add => _inner.OnMakingApiRequest += value;
        remove => _inner.OnMakingApiRequest -= value;
    }

    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived
    {
        add => _inner.OnApiResponseReceived += value;
        remove => _inner.OnApiResponseReceived -= value;
    }

    public Task<bool> TestApi(CancellationToken cancellationToken = default)
        => _inner.TestApi(cancellationToken);

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default)
        => _inner.DownloadFile(filePath, destination, cancellationToken);

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default)
        => _inner.DownloadFile(file, destination, cancellationToken);
}
