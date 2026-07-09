namespace Kippo.Callbacks;

/// <summary>
/// Wire-format helpers shared by the keyboard builder (which writes vault tokens) and the vault
/// middleware (which resolves them). A vaulted button carries <c>kv:&lt;token&gt;</c> as its callback
/// data; the token resolves to an envelope packing the routing key and the JSON payload.
/// </summary>
internal static class CallbackVault
{
    /// <summary>Marker prefix identifying a vaulted callback token on the wire.</summary>
    public const string TokenPrefix = "kv:";

    /// <summary>Context.Items key under which the rehydrated payload JSON is stashed for the router.</summary>
    public const string PayloadItemKey = "__kippo_vault_payload";

    // Unit Separator — a control char that never appears in a routing key or JSON text.
    private const char Separator = '';

    public static string PackEnvelope(string route, string payloadJson) => route + Separator + payloadJson;

    public static bool TryUnpackEnvelope(string envelope, out string route, out string payloadJson)
    {
        var idx = envelope.IndexOf(Separator);
        if (idx < 0)
        {
            route = envelope;
            payloadJson = string.Empty;
            return false;
        }

        route = envelope[..idx];
        payloadJson = envelope[(idx + 1)..];
        return true;
    }

    public static bool IsVaultToken(string? callbackData)
        => callbackData is not null && callbackData.StartsWith(TokenPrefix, StringComparison.Ordinal);

    public static string ExtractToken(string callbackData) => callbackData[TokenPrefix.Length..];
}
