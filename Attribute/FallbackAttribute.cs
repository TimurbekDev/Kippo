namespace Kippo.Attribute;

/// <summary>
/// Marks a method as the fallback handler, invoked when an update matches no
/// command, callback, text, contact or chat-member handler.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FallbackAttribute : System.Attribute
{
}
