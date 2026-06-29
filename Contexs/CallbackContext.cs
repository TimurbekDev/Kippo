namespace Kippo.Contexs;

public sealed class CallbackContext
{
    private readonly Context _context;

    internal CallbackContext(Context contex)
    {
        _context = contex;
    }

    public Task Answer(string? text = null,bool showAlert = false,string? url = null, int? cacheTime = null)
    {
        var callback = _context.Update.CallbackQuery;
        if (callback == null)
            return Task.CompletedTask;

        return _context.BotClient.AnswerCallbackQuery(callback.Id,text,showAlert,url,cacheTime);
    }

    public string Data => _context.Update.CallbackQuery?.Data 
        ?? throw new InvalidOperationException(
            "CallbackQuery.Data is null. Ensure this property is only accessed for callback queries with data. " +
            $"Current update type: {_context.Update.Type}"
        );
}
