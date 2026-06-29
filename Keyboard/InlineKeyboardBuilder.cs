namespace Kippo.Keyboard;

public class InlineKeyboardBuilder
{
    private readonly List<List<InlineKeyboardButton>> _rows = new();
    private List<InlineKeyboardButton> _currentRow = new();

    public static InlineKeyboardBuilder Create() => new InlineKeyboardBuilder();

    public InlineKeyboardBuilder Button(string text, string callbackData)
    {
        _currentRow.Add(InlineKeyboardButton.WithCallbackData(text, callbackData));
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