namespace Jmap;

/// <summary>
/// A push notification (RFC 8620 §7.1): per account, the new state string for each data
/// type that changed (e.g. "Email", "Mailbox"). Compare against your last-known states and
/// fetch /changes for the ones that moved.
/// </summary>
public sealed record StateChange
{
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Changed { get; init; }

    /// <summary>Over WebSocket only (RFC 8887 §4.3.5.2): opaque marker to resume push
    /// delivery from this point when reconnecting.</summary>
    public string? PushState { get; init; }
}
