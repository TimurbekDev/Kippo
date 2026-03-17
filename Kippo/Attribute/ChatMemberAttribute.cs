namespace Kippo.Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ChatMemberAttribute : System.Attribute
{
    public ChatMemberAttribute()
    {
    }
}
