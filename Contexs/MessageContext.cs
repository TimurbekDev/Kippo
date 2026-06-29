namespace Kippo.Contexs;

public sealed class MessageContext
{
    private readonly Context _context;

    public MessageContext(Context context)
    {
        _context = context;
    }

    public string Text => _context.Update.Message?.Text
        ?? throw new InvalidOperationException(
            "Message.Text is null. Ensure this property is only accessed for text messages. " +
            $"Current update type: {_context.Update.Type}, " +
            $"Message type: {_context.Update.Message?.Type.ToString() ?? "null"}"
        );

    public Contact Contact => _context.Update.Message?.Contact
        ?? throw new InvalidOperationException(
            "Message.Contact is null. Ensure this property is only accessed for contact messages. " +
            $"Current update type: {_context.Update.Type}, " +
            $"Message type: {_context.Update.Message?.Type.ToString() ?? "null"}"
        );
}
