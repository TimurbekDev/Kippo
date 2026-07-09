namespace Kippo.Client;

/// <summary>
/// Configures how <see cref="ThrottlingBotClient"/> reacts to Telegram flood limits.
/// </summary>
public class FloodControlOptions
{
    /// <summary>
    /// Maximum number of times a single outbound request is retried after receiving
    /// a <c>429 Too Many Requests</c>. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Upper bound on how long a single <c>retry_after</c> wait may be honored.
    /// If Telegram asks for a longer pause the request fails fast instead of blocking. Defaults to 60s.
    /// </summary>
    public TimeSpan MaxRetryAfter { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Minimum spacing between two outbound requests targeting the same chat.
    /// <see cref="TimeSpan.Zero"/> (default) disables per-chat throttling. Telegram's
    /// practical ceiling is ~1 message/second per chat, so <c>TimeSpan.FromSeconds(1)</c>
    /// is a safe value for high-traffic bots.
    /// </summary>
    public TimeSpan MinIntervalPerChat { get; set; } = TimeSpan.Zero;
}
