using System.Reflection;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;

namespace Kippo.Testing;

/// <summary>
/// A fake <see cref="ITelegramBotClient"/> that records every outbound request instead of hitting
/// Telegram's servers. Extension methods such as <c>SendMessage</c> / <c>AnswerCallbackQuery</c> all
/// funnel through <see cref="SendRequest{TResponse}"/>, so their request DTOs are captured here for
/// inspection in tests. Responses are fabricated locally: message-returning calls yield a synthetic
/// <see cref="Message"/>, boolean calls return <c>true</c>.
/// </summary>
public sealed class RecordingBotClient : ITelegramBotClient
{
    private readonly List<IRequest> _sent = new();
    private int _messageId;

    /// <summary>The bot's own identity used when fabricating outgoing messages.</summary>
    public User BotUser { get; set; } = new() { Id = 42, IsBot = true, FirstName = "TestBot", Username = "test_bot" };

    /// <summary>Every request the code under test sent, in order.</summary>
    public IReadOnlyList<IRequest> Sent => _sent;

    /// <summary>Captured requests of a specific DTO type (e.g. <c>SentOf&lt;SendMessageRequest&gt;()</c>).</summary>
    public IEnumerable<TRequest> SentOf<TRequest>() where TRequest : IRequest => _sent.OfType<TRequest>();

    /// <summary>Removes all recorded requests. Useful between logical steps of a scenario.</summary>
    public void Clear() => _sent.Clear();

    public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        _sent.Add(request);
        return Task.FromResult((TResponse)Fabricate(request, typeof(TResponse))!);
    }

    private object? Fabricate(object request, Type responseType)
    {
        if (responseType == typeof(Message))
        {
            return new Message
            {
                Id = Interlocked.Increment(ref _messageId),
                Date = DateTime.UtcNow,
                From = BotUser,
                Text = ReadString(request, "Text"),
                Chat = new Chat
                {
                    Id = TryGetChatId(request) ?? 0,
                    Type = ChatType.Private
                }
            };
        }

        if (responseType == typeof(bool))
            return true;

        return responseType.IsValueType ? Activator.CreateInstance(responseType) : null;
    }

    private static string? ReadString(object request, string property)
        => request.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(request) as string;

    private static long? TryGetChatId(object request)
        => request is IChatTargetable { ChatId.Identifier: long id } ? id : null;

    // --- Inert members: a recording client never talks to the network ---

    public bool LocalBotServer => false;
    public long BotId => BotUser.Id;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);
    public IExceptionParser ExceptionsParser { get; set; } = default!;

#pragma warning disable CS0067 // events are part of the interface contract but never raised by a fake
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;
    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;
#pragma warning restore CS0067

    public Task<bool> TestApi(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DownloadFile(TGFile file, Stream destination, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
