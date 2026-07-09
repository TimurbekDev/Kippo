using Kippo.Callbacks;
using Kippo.Keyboard;
using Kippo.Scenes;
using Kippo.SessionStorage;

namespace Kippo.Contexs;

public class Context
{
    public Update Update { get; }
    public ITelegramBotClient BotClient { get; }
    public CancellationToken CancellationToken { get; }
    public ISessionStore SessionStore { get; }
    public IServiceProvider? ServiceProvider { get; }

    /// <summary>Per-update scratch space shared across middleware and the router (e.g. vaulted callback payloads).</summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    // Callback context
    public CallbackContext Callback {  get; }

    //Message context
    public MessageContext Message { get;  }

    //Session
    public Session? Session { get; set; }

    public Context(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken,ISessionStore sessionStore, IServiceProvider? serviceProvider = null)
    {
        BotClient = botClient;
        Update = update;
        CancellationToken = cancellationToken;
        Callback = new CallbackContext(this);
        Message = new MessageContext(this);
        SessionStore = sessionStore;
        ServiceProvider = serviceProvider;
    }


    public long ChatId
    {
        get
        {
            if (Update.Message != null)
                return Update.Message.Chat.Id;
            
            if (Update.EditedMessage != null)
                return Update.EditedMessage.Chat.Id;

            if (Update.CallbackQuery?.Message != null)
                return Update.CallbackQuery.Message.Chat.Id;
            
            if (Update.MyChatMember != null)
                return Update.MyChatMember.Chat.Id;
            
            if (Update.ChatMember != null)
                return Update.ChatMember.Chat.Id;

            throw new InvalidOperationException(
                $"Update type '{Update.Type}' does not contain a chat id or is not supported."
            );
        }
    }

    public Task Reply(string text, ReplyMarkup? replyMarkup = null, ParseMode? parseMode = null)
    {
        return BotClient.SendMessage(
            chatId: ChatId,
            text: text,
            replyMarkup: replyMarkup,
            parseMode:parseMode ?? ParseMode.None,
            cancellationToken: CancellationToken);
    }

    /// <summary>
    /// Creates an inline keyboard builder bound to the callback-data vault, enabling
    /// <c>.Payload(...)</c> buttons whose payload exceeds Telegram's 64-byte callback limit.
    /// Requires <c>ICallbackStore</c> in DI (registered automatically by <c>AddKippo</c>).
    /// </summary>
    public InlineKeyboardBuilder Inline()
        => new(ServiceProvider?.GetService(typeof(ICallbackStore)) as ICallbackStore);

    /// <summary>
    /// Enters the named conversation scene. The scene's first prompt is sent immediately; the user's
    /// subsequent text replies are routed into the scene until it completes or <see cref="ExitScene"/>
    /// is called. See <c>[Scene]</c> and <c>SceneContext</c>.
    /// </summary>
    public void EnterScene(string name)
    {
        if (Session is null)
            throw new InvalidOperationException(
                "EnterScene requires an active session. Register SessionMiddleware via AddKippoMiddleware<SessionMiddleware>().");

        SceneState.Enter(Session, name);
        Items[SceneState.EnterFlagKey] = name;
    }

    /// <summary>Exits the active scene, if any (e.g. from a /cancel command). Safe to call when no scene is active.</summary>
    public void ExitScene()
    {
        if (Session is not null)
            SceneState.Clear(Session);
    }

    /// <summary>True when the user is currently inside a scene.</summary>
    public bool InScene => SceneState.IsActive(Session);
}
