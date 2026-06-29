namespace Kippo.Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CallbackQueryAttribute : System.Attribute
{
    public string Pattern { get; }

    public CallbackQueryAttribute(string pattern)
    {
        Pattern = pattern;
    }

    public bool Matches(string? callbackData)
    {
        if (string.IsNullOrEmpty(callbackData))
            return false;

        if (Pattern == "*")
            return true;

        if (Pattern.EndsWith("*"))
        {
            var prefix = Pattern[..^1];
            return callbackData.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(Pattern, callbackData, StringComparison.OrdinalIgnoreCase);
    }
}
