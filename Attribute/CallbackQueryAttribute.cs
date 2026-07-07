using System.Text;
using System.Text.RegularExpressions;

namespace Kippo.Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CallbackQueryAttribute : System.Attribute
{
    public string Pattern { get; }

    private readonly Regex? _templateRegex;
    private readonly bool _hasTemplate;

    public CallbackQueryAttribute(string pattern)
    {
        Pattern = pattern;

        if (pattern.Contains('{') && pattern.Contains('}'))
        {
            _hasTemplate = true;
            _templateRegex = BuildTemplateRegex(pattern);
        }
    }

    public bool Matches(string? callbackData)
    {
        return TryMatch(callbackData, out _);
    }

    /// <summary>
    /// Matches the callback data against the pattern and extracts any named
    /// placeholder values declared in the template (e.g. <c>product:{id}</c>).
    /// </summary>
    public bool TryMatch(string? callbackData, out IReadOnlyDictionary<string, string> values)
    {
        values = EmptyValues;

        if (string.IsNullOrEmpty(callbackData))
            return false;

        if (_hasTemplate)
        {
            var match = _templateRegex!.Match(callbackData);
            if (!match.Success)
                return false;

            var captured = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in _templateRegex.GetGroupNames())
            {
                if (int.TryParse(name, out _))
                    continue; // skip the implicit numbered groups

                captured[name] = match.Groups[name].Value;
            }

            values = captured;
            return true;
        }

        if (Pattern == "*")
            return true;

        if (Pattern.EndsWith("*"))
        {
            var prefix = Pattern[..^1];
            return callbackData.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(Pattern, callbackData, StringComparison.OrdinalIgnoreCase);
    }

    private static Regex BuildTemplateRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        int i = 0;

        while (i < pattern.Length)
        {
            char c = pattern[i];

            if (c == '{')
            {
                int end = pattern.IndexOf('}', i);
                if (end < 0)
                    throw new ArgumentException(
                        $"Unterminated placeholder in callback pattern '{pattern}'. Expected a closing '}}'.");

                var name = pattern[(i + 1)..end];
                if (name.Length == 0)
                    throw new ArgumentException(
                        $"Empty placeholder name in callback pattern '{pattern}'.");

                sb.Append("(?<").Append(name).Append(">.+?)");
                i = end + 1;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
                i++;
            }
        }

        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyValues =
        new Dictionary<string, string>();
}
