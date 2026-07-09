using System.Text.Json;
using Kippo.Extensions;
using Kippo.SessionStorage;

namespace Kippo.Scenes;

/// <summary>
/// Reads and writes the active-scene bookkeeping stored in the user's <see cref="Session"/>:
/// the scene name and the ordered list of answers collected so far. Persisted as plain strings so
/// it round-trips through any <c>ISessionStore</c> (in-memory, Redis, database).
/// </summary>
internal static class SceneState
{
    private const string NameKey = "__kippo_scene";
    private const string AnswersKey = "__kippo_scene_answers";

    /// <summary>Context.Items flag set by <c>EnterScene</c> so the router runs the first turn.</summary>
    public const string EnterFlagKey = "__kippo_scene_enter";

    public static string? GetName(Session session) => session.Get<string>(NameKey);

    public static bool IsActive(Session? session)
        => session is not null && !string.IsNullOrEmpty(GetName(session));

    public static void Enter(Session session, string name)
    {
        session.Set(NameKey, name);
        session.Set(AnswersKey, "[]");
    }

    public static List<string> LoadAnswers(Session session)
    {
        var json = session.Get<string>(AnswersKey);
        if (string.IsNullOrEmpty(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void SaveAnswers(Session session, List<string> answers)
        => session.Set(AnswersKey, JsonSerializer.Serialize(answers));

    public static void Clear(Session session)
    {
        session.Remove(NameKey);
        session.Remove(AnswersKey);
    }
}
