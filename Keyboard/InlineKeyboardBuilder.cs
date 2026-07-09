using System.Text.Json;
using Kippo.Callbacks;

namespace Kippo.Keyboard;

public class InlineKeyboardBuilder
{
    private readonly List<List<InlineKeyboardButton>> _rows = new();
    private List<InlineKeyboardButton> _currentRow = new();
    private readonly ICallbackStore? _callbackStore;

    public InlineKeyboardBuilder()
    {
    }

    public InlineKeyboardBuilder(ICallbackStore? callbackStore)
    {
        _callbackStore = callbackStore;
    }

    public static InlineKeyboardBuilder Create() => new InlineKeyboardBuilder();

    public InlineKeyboardBuilder Button(string text, string callbackData)
    {
        _currentRow.Add(InlineKeyboardButton.WithCallbackData(text, callbackData));
        return this;
    }

    /// <summary>
    /// Adds a button carrying an arbitrarily large typed <paramref name="payload"/>. The payload is
    /// stored in the callback vault and only a short token travels on the wire, so it is never subject
    /// to Telegram's 64-byte <c>callback_data</c> limit. The button routes to a handler matching
    /// <paramref name="route"/>; the payload is bound to a handler parameter of the payload's type.
    /// </summary>
    /// <param name="text">Button label.</param>
    /// <param name="route">Routing key matched by a <c>[CallbackQuery(route)]</c> handler.</param>
    /// <param name="payload">Any JSON-serializable object; bound to the handler by parameter type.</param>
    public InlineKeyboardBuilder Payload(string text, string route, object payload)
    {
        if (_callbackStore is null)
            throw new InvalidOperationException(
                "Payload buttons require an ICallbackStore. Build the keyboard with context.Inline() " +
                "so the vault is available (AddKippo registers it automatically).");

        var envelope = CallbackVault.PackEnvelope(route, JsonSerializer.Serialize(payload));
        var token = _callbackStore.Save(envelope);
        _currentRow.Add(InlineKeyboardButton.WithCallbackData(text, CallbackVault.TokenPrefix + token));
        return this;
    }

    public InlineKeyboardBuilder UrlButton(string text, string url)
    {
        _currentRow.Add(InlineKeyboardButton.WithUrl(text, url));
        return this;
    }

    public InlineKeyboardBuilder Row()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
            _currentRow = new List<InlineKeyboardButton>();
        }
        return this;
    }

    public InlineKeyboardMarkup Build()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
            _currentRow = new List<InlineKeyboardButton>();
        }
        return new InlineKeyboardMarkup(_rows);
    }
}