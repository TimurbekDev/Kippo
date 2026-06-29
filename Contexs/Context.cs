using Kippo.SessionStorage;

namespace Kippo.Contexs;

public class Context
{
    public Update Update { get; }
    public ITelegramBotClient BotClient { get; }
    public CancellationToken CancellationToken { get; }
    public ISessionStore SessionStore { get; }
    public IServiceProvider? ServiceProvider { get; }

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
}
