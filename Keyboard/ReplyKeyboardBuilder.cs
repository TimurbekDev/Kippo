namespace Kippo.Keyboard;

public class ReplyKeyboardBuilder
{
    private bool _resizeKeyboard = false;
    private bool _oneTimeKeyboard = false;
    private string? _inputFieldPlaceholder = null;
    private List<KeyboardButton> _currentRow = new();
    private readonly List<List<KeyboardButton>> _rows = new();
    public static ReplyKeyboardBuilder Create() => new ReplyKeyboardBuilder();

    public ReplyKeyboardBuilder Button(string text)
    {
        _currentRow.Add(new KeyboardButton(text));
        return this;
    }

    public ReplyKeyboardBuilder LocationButton(string text)
    {
        _currentRow.Add(KeyboardButton.WithRequestLocation(text));
        return this;
    }

    public ReplyKeyboardBuilder ContactButton(string text)
    {
        _currentRow.Add(KeyboardButton.WithRequestContact(text));
        return this;
    }

    public ReplyKeyboardBuilder Row()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
            _currentRow = new List<KeyboardButton>();
        }
        return this;
    }

    public ReplyKeyboardBuilder Resize(bool resize = true)
    {
        _resizeKeyboard = resize;
        return this;
    }

    public ReplyKeyboardBuilder OneTime(bool oneTime = true)
    {
        _oneTimeKeyboard = oneTime;
        return this;
    }

    public ReplyKeyboardBuilder Placeholder(string placeholder)
    {
        _inputFieldPlaceholder = placeholder;
        return this;
    }

    public ReplyKeyboardMarkup Build()
    {
        if (_currentRow.Count > 0)
        {
            _rows.Add(_currentRow);
        }

        return new ReplyKeyboardMarkup(_rows)
        {
            ResizeKeyboard = _resizeKeyboard,
            OneTimeKeyboard = _oneTimeKeyboard,
            InputFieldPlaceholder = _inputFieldPlaceholder
        };
    }
}
