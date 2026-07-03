namespace Jmap;

/// <summary>
/// A push notification (RFC 8620 §7.1): per account, the new state string for each data
/// type that changed (e.g. "Email", "Mailbox"). Compare against your last-known states and
/// fetch /changes for the ones that moved.
/// </summary>
public sealed record StateChange
{
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Changed { get; init; }
}
