namespace Kippo.Attribute;

/// <summary>
/// Marks a method as a conversation scene — a linear, multi-step dialog written as ordinary
/// sequential code with <c>await ctx.Ask(...)</c>. Enter it from any handler via
/// <c>context.EnterScene("name")</c>. While a scene is active, the user's text replies are fed back
/// into the scene instead of the normal routing table until the scene completes or is exited.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SceneAttribute : System.Attribute
{
    /// <summary>Unique name used to enter the scene (<c>EnterScene(name)</c>).</summary>
    public string Name { get; }

    public SceneAttribute(string name)
    {
        Name = name;
    }
}
