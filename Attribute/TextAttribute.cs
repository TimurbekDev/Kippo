namespace Kippo.Attribute;


[AttributeUsage(AttributeTargets.Method)]
public class TextAttribute : System.Attribute
{
    public string? State { get; init; }
    public string? Pattern { get; init; }
    public string? Contains { get; init; }
    public string? Regex { get; init; }

    internal int Priority =>
        State != null && HasMatcher ? 1 :
        State != null ? 2 :
        HasMatcher ? 3 :
        4;

    internal bool HasMatcher =>
        Pattern != null || Contains != null || Regex != null;
}
