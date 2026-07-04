using System.Text.Json.Serialization;
using Jmap.Json;

namespace Jmap;

/// <summary>
/// A registration for server-initiated Web Push (RFC 8620 §7.2). Subscriptions belong to
/// the credentials, not to an account, so their /get and /set methods take no accountId.
/// </summary>
public sealed record PushSubscription
{
    public string? Id { get; init; }

    /// <summary>An id unique to this client+device, so a client can find its own subscription.</summary>
    public string? DeviceClientId { get; init; }

    /// <summary>The push endpoint URL the server POSTs notifications to (RFC 8030).</summary>
    public string? Url { get; init; }

    /// <summary>Keys for encrypted pushes (RFC 8291); null for unencrypted.</summary>
    public PushSubscriptionKeys? Keys { get; init; }

    /// <summary>Echo back the code delivered via the push channel to prove the URL is ours.</summary>
    public string? VerificationCode { get; init; }

    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? Expires { get; init; }

    /// <summary>Data type names to be notified about (e.g. "Email"); null means all.</summary>
    public IReadOnlyList<string>? Types { get; init; }
}

/// <summary>The client's P-256 ECDH public key and auth secret, base64url-encoded (RFC 8291).</summary>
public sealed record PushSubscriptionKeys(string P256dh, string Auth);

/// <summary>
/// The object pushed to a new subscription's URL to prove ownership (RFC 8620 §7.2):
/// write its code back to the subscription's verificationCode to activate it.
/// </summary>
public sealed record PushVerification
{
    public required string PushSubscriptionId { get; init; }
    public required string VerificationCode { get; init; }
}

// ── Method shapes (RFC 8620 §7.2.1–7.2.2): standard /get and /set minus the account and
//    state arguments, because subscriptions are per-credential and unsynchronised. ────────

public sealed record PushSubscriptionGetArguments
{
    public IReadOnlyList<string>? Ids { get; init; }
    public IReadOnlyList<string>? Properties { get; init; }
}

public sealed record PushSubscriptionGetResponse
{
    public required IReadOnlyList<PushSubscription> List { get; init; }
    public IReadOnlyList<string> NotFound { get; init; } = [];
}

/// <summary>Only verificationCode, expires and types may appear in update patches.</summary>
public sealed record PushSubscriptionSetArguments
{
    public IReadOnlyDictionary<string, PushSubscription>? Create { get; init; }
    public IReadOnlyDictionary<string, PatchObject>? Update { get; init; }
    public IReadOnlyList<string>? Destroy { get; init; }
}

public sealed record PushSubscriptionSetResponse
{
    public IReadOnlyDictionary<string, PushSubscription>? Created { get; init; }
    public IReadOnlyDictionary<string, System.Text.Json.JsonElement?>? Updated { get; init; }
    public IReadOnlyList<string>? Destroyed { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCreated { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotUpdated { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotDestroyed { get; init; }
}
