namespace Kippo.Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute : System.Attribute
{
    public string Command { get; }

    public string? Description { get; set; }

    public CommandAttribute(string command)
    {
        Command = command.TrimStart('/');
    }
}
