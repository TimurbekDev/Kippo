using Kippo.Callbacks;
using Kippo.Handlers;
using Kippo.Middleware;
using Kippo.SessionStorage;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Requests;

namespace Kippo.Testing;

/// <summary>
/// An in-memory test host for a Kippo <see cref="BotUpdateHandler"/>. It wires up the real router,
/// middleware pipeline and session store around a <see cref="RecordingBotClient"/>, then lets a test
/// feed synthetic updates (text, commands, button taps, contacts) and assert on what the bot sent —
/// with no bot token and no network.
/// </summary>
/// <typeparam name="THandler">The handler under test.</typeparam>
public sealed class TestBot<THandler> where THandler : BotUpdateHandler
{
    private readonly THandler _handler;
    private readonly long _chatId;
    private readonly User _user;
    private int _updateId;
    private int _messageId;

    /// <summary>The fake client capturing everything the bot sends.</summary>
    public RecordingBotClient Client { get; } = new();

    /// <summary>The in-memory session store shared with the handler.</summary>
    public ISessionStore SessionStore { get; }

    /// <summary>The root service provider backing the handler (dispose it via the test's teardown if needed).</summary>
    public IServiceProvider Services { get; }

    /// <param name="configureServices">Register the fakes/services the handler resolves (DbContext, repositories, ...).</param>
    /// <param name="chatId">The chat all synthetic updates originate from.</param>
    /// <param name="userId">The user all synthetic updates originate from.</param>
    public TestBot(Action<IServiceCollection>? configureServices = null, long chatId = 1000, long userId = 1000)
    {
        _chatId = chatId;
        _user = new User { Id = userId, IsBot = false, FirstName = "Tester", Username = "tester" };

        var services = new ServiceCollection();
        SessionStore = new InMemorySessionStore();
        services.AddSingleton(SessionStore);
        services.AddSingleton<ITelegramBotClient>(Client);
        services.AddSingleton<ICallbackStore>(new InMemoryCallbackStore());
        services.AddSingleton<IBotMiddleware, CallbackVaultMiddleware>();
        services.AddSingleton<IBotMiddleware, SessionMiddleware>();
        configureServices?.Invoke(services);

        Services = services.BuildServiceProvider();

        _handler = ActivatorUtilities.CreateInstance<THandler>(Services);
        _handler.Initialize(SessionStore, Services.GetServices<IBotMiddleware>(), logger: null, serviceProvider: Services);
    }

    /// <summary>The most recent text message the bot sent, or <c>null</c> if it sent none.</summary>
    public SendMessageRequest? LastReply => Client.SentOf<SendMessageRequest>().LastOrDefault();

    /// <summary>Every text message the bot has sent so far, in order.</summary>
    public IReadOnlyList<SendMessageRequest> Replies => Client.SentOf<SendMessageRequest>().ToList();

    /// <summary>The current session for the test chat.</summary>
    public Session Session => SessionStore.GetAsync(_chatId).GetAwaiter().GetResult();

    /// <summary>Sends a plain text message (or a command if <paramref name="text"/> starts with '/').</summary>
    public Task SendText(string text)
    {
        var update = new Update
        {
            Id = ++_updateId,
            Message = new Message
            {
                Id = ++_messageId,
                Date = DateTime.UtcNow,
                Text = text,
                From = _user,
                Chat = new Chat { Id = _chatId, Type = ChatType.Private }
            }
        };
        return Dispatch(update);
    }

    /// <summary>Sends a slash command, e.g. <c>SendCommand("start")</c> or <c>SendCommand("echo", "hi")</c>.</summary>
    public Task SendCommand(string command, string? args = null)
        => SendText(string.IsNullOrEmpty(args) ? $"/{command}" : $"/{command} {args}");

    /// <summary>Simulates a user tapping an inline button carrying the given callback data.</summary>
    public Task TapButton(string callbackData)
    {
        var update = new Update
        {
            Id = ++_updateId,
            CallbackQuery = new CallbackQuery
            {
                Id = Guid.NewGuid().ToString("N"),
                Data = callbackData,
                From = _user,
                Message = new Message
                {
                    Id = ++_messageId,
                    Date = DateTime.UtcNow,
                    From = Client.BotUser,
                    Chat = new Chat { Id = _chatId, Type = ChatType.Private }
                }
            }
        };
        return Dispatch(update);
    }

    /// <summary>Simulates the user sharing a phone contact.</summary>
    public Task SendContact(string phoneNumber, string? firstName = null)
    {
        var update = new Update
        {
            Id = ++_updateId,
            Message = new Message
            {
                Id = ++_messageId,
                Date = DateTime.UtcNow,
                From = _user,
                Chat = new Chat { Id = _chatId, Type = ChatType.Private },
                Contact = new Contact
                {
                    PhoneNumber = phoneNumber,
                    FirstName = firstName ?? _user.FirstName,
                    UserId = _user.Id
                }
            }
        };
        return Dispatch(update);
    }

    private Task Dispatch(Update update)
        => _handler.HandleUpdateAsync(Client, update, CancellationToken.None);
}
