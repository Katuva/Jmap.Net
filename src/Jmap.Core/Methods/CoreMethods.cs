namespace Jmap;

/// <summary>Every method name RFC 8620 defines, plus the blob-management (RFC 9404) and
/// quota (RFC 9425) extensions, for building <see cref="Invocation"/>s.</summary>
public static class CoreMethods
{
    /// <summary>Round-trips its arguments unchanged (RFC 8620 §4) — a connectivity test.</summary>
    public const string CoreEcho = "Core/echo";

    public const string BlobCopy = "Blob/copy";

    public const string PushSubscriptionGet = "PushSubscription/get";
    public const string PushSubscriptionSet = "PushSubscription/set";

    // RFC 9404 (urn:ietf:params:jmap:blob)
    public const string BlobUpload = "Blob/upload";
    public const string BlobGet = "Blob/get";
    public const string BlobLookup = "Blob/lookup";

    // RFC 9425 (urn:ietf:params:jmap:quota)
    public const string QuotaGet = "Quota/get";
    public const string QuotaChanges = "Quota/changes";
    public const string QuotaQuery = "Quota/query";
    public const string QuotaQueryChanges = "Quota/queryChanges";
}

/// <summary>Blob/copy (RFC 8620 §6.3): copies blobs between accounts on the same server.</summary>
public sealed record BlobCopyArguments
{
    public required string FromAccountId { get; init; }
    public required string AccountId { get; init; }
    public required IReadOnlyList<string> BlobIds { get; init; }
}

public sealed record BlobCopyResponse
{
    public required string FromAccountId { get; init; }
    public required string AccountId { get; init; }
    /// <summary>Source blob id → blob id in the destination account.</summary>
    public IReadOnlyDictionary<string, string>? Copied { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCopied { get; init; }
}
