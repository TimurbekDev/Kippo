using System.Globalization;
using Kippo.Contexs;
using Kippo.SessionStorage;

namespace Kippo.Scenes;

/// <summary>
/// The context passed to a <c>[Scene]</c> method. Model the dialog as ordinary sequential code:
/// each <see cref="Ask(string)"/> sends a prompt and returns the user's next reply.
/// <para>
/// Under the hood the scene method is re-run from the top on every incoming message. Answers already
/// collected are replayed instantly from the session, so only the next unanswered <c>Ask</c> actually
/// prompts the user. Side effects issued through <see cref="Reply"/> are suppressed during replay, so
/// each message produces exactly one new prompt — no duplicated messages.
/// </para>
/// </summary>
public sealed class SceneContext
{
    private readonly Context _ctx;
    private readonly string? _pending;
    private bool _consumed;
    private bool _replaying;
    private int _asksSeen;

    internal List<string> Answers { get; }

    internal SceneContext(Context ctx, List<string> answers, string? pendingInput)
    {
        _ctx = ctx;
        Answers = answers;
        _pending = pendingInput;
        // If this turn carries a fresh input, we are replaying prior answers until we consume it.
        // On initial entry (no pending input) nothing has run yet, so we are already "live".
        _replaying = pendingInput != null;
    }

    /// <summary>The underlying update context (bot client, update, chat id, services).</summary>
    public Context Context => _ctx;

    /// <summary>The current user session.</summary>
    public Session Session => _ctx.Session!;

    /// <summary>Sends <paramref name="prompt"/> and returns the user's next text reply.</summary>
    public Task<string> Ask(string prompt)
    {
        if (_asksSeen < Answers.Count)
        {
            var replayed = Answers[_asksSeen];
            _asksSeen++;
            return Task.FromResult(replayed);
        }

        if (_pending != null && !_consumed)
        {
            Consume(_pending);
            return Task.FromResult(_pending);
        }

        return PromptAndHalt<string>(prompt);
    }

    /// <summary>
    /// Sends <paramref name="prompt"/> and returns the user's next reply parsed as
    /// <typeparamref name="T"/> (int, long, Guid, enum, decimal, …). If parsing fails, the bot sends
    /// <paramref name="retry"/> (or re-sends the prompt) and waits for another reply.
    /// </summary>
    public Task<T> Ask<T>(string prompt, string? retry = null)
    {
        if (_asksSeen < Answers.Count)
        {
            var replayed = Answers[_asksSeen];
            _asksSeen++;
            // Stored answers were validated when accepted, so this conversion succeeds.
            TryConvert(replayed, typeof(T), out var value);
            return Task.FromResult((T)value!);
        }

        if (_pending != null && !_consumed)
        {
            if (TryConvert(_pending, typeof(T), out var parsed))
            {
                Consume(_pending);
                return Task.FromResult((T)parsed!);
            }

            // Invalid input: do not consume — re-ask the same step next turn.
            return PromptAndHalt<T>(retry ?? prompt);
        }

        return PromptAndHalt<T>(prompt);
    }

    /// <summary>
    /// Sends a message as part of the scene. Suppressed while the scene is replaying past
    /// already-answered steps, so it fires exactly once per logical step.
    /// </summary>
    public Task Reply(string text, ReplyMarkup? replyMarkup = null, ParseMode? parseMode = null)
        => _replaying ? Task.CompletedTask : _ctx.Reply(text, replyMarkup, parseMode);

    private void Consume(string input)
    {
        Answers.Add(input);
        _consumed = true;
        _replaying = false;
        _asksSeen++;
    }

    private async Task<T> PromptAndHalt<T>(string prompt)
    {
        // Prompts and retries always send — they are the intended effect of reaching this step.
        await _ctx.Reply(prompt);
        throw new SceneHaltException();
    }

    internal static bool TryConvert(string raw, Type type, out object? value)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            if (target == typeof(string))
                value = raw;
            else if (target.IsEnum)
                value = Enum.Parse(target, raw, ignoreCase: true);
            else if (target == typeof(Guid))
                value = Guid.Parse(raw);
            else
                value = Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);

            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}

/// <summary>Thrown internally to suspend a scene at an unanswered <c>Ask</c>. Caught by the router.</summary>
internal sealed class SceneHaltException : Exception
{
}
