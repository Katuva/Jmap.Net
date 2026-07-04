using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jmap;

// The blob-management extension (RFC 9404, urn:ietf:params:jmap:blob): create blobs
// in-band with Blob/upload, read octets/digests with Blob/get, and reverse-map blobs to
// the objects referencing them with Blob/lookup.

/// <summary>The blob capability object (RFC 9404 §3.1), found per account in
/// <c>accountCapabilities</c>; the session-level object is empty.</summary>
public sealed record BlobCapability
{
    /// <summary>Largest blob a client may create in-band; null means no advertised limit.</summary>
    public long? MaxSizeBlobSet { get; init; }
    /// <summary>Max <see cref="BlobDataSource"/>s per Blob/upload creation (at least 64).</summary>
    public long MaxDataSources { get; init; }
    /// <summary>Type names usable with Blob/lookup; empty when lookups are unsupported.</summary>
    public IReadOnlyList<string> SupportedTypeNames { get; init; } = [];
    /// <summary>Lowercased digest algorithms for Blob/get (e.g. "sha-256"); prefer earlier entries.</summary>
    public IReadOnlyList<string> SupportedDigestAlgorithms { get; init; } = [];
}

/// <summary>Blob/upload (RFC 9404 §4.1): a create-only /set.</summary>
public sealed record BlobUploadArguments
{
    public required string AccountId { get; init; }
    /// <summary>Creation-id → upload; successful ids join the request's createdIds, so later
    /// calls in the same request can reference them as "#creationId".</summary>
    public required IReadOnlyDictionary<string, BlobUploadObject> Create { get; init; }
}

public sealed record BlobUploadObject
{
    /// <summary>Sources concatenated in order; empty creates an empty blob.</summary>
    public required IReadOnlyList<BlobDataSource> Data { get; init; }
    /// <summary>Media type hint for the created blob.</summary>
    public string? Type { get; init; }
}

/// <summary>One source of octets for Blob/upload (RFC 9404 §4.1): literal text, literal
/// base64, or a range of an existing blob — set exactly one via the factory methods.</summary>
public sealed record BlobDataSource
{
    [JsonPropertyName("data:asText")]
    public string? DataAsText { get; init; }

    [JsonPropertyName("data:asBase64")]
    public string? DataAsBase64 { get; init; }

    /// <summary>May be a "#creationId" back-reference to a blob made earlier in the request.</summary>
    public string? BlobId { get; init; }
    public long? Offset { get; init; }
    public long? Length { get; init; }

    public static BlobDataSource Text(string value) => new() { DataAsText = value };

    public static BlobDataSource Base64(ReadOnlySpan<byte> octets) => new() { DataAsBase64 = Convert.ToBase64String(octets) };

    public static BlobDataSource FromBlob(string blobId, long? offset = null, long? length = null)
        => new() { BlobId = blobId, Offset = offset, Length = length };
}

/// <summary>Blob/upload response. Blobs carry no state, so unlike a /set there is none.</summary>
public sealed record BlobUploadResponse
{
    public required string AccountId { get; init; }
    public IReadOnlyDictionary<string, BlobUploadResult>? Created { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCreated { get; init; }
}

public sealed record BlobUploadResult
{
    /// <summary>The blob id of the created blob.</summary>
    public required string Id { get; init; }
    public string? Type { get; init; }
    public long Size { get; init; }
}

/// <summary>Blob/get (RFC 9404 §4.2): fetch blob octets, size and digests via a /get.</summary>
public sealed record BlobGetArguments : GetArguments
{
    /// <summary>Start of the selected range; null means 0.</summary>
    public long? Offset { get; init; }
    /// <summary>Octets to return from the offset; null means the rest of the blob.</summary>
    public long? Length { get; init; }
}

/// <summary>The property names Blob/get accepts; default (null) is data and size.</summary>
public static class BlobGetProperties
{
    /// <summary>Returns data:asText when the range is valid UTF-8, else data:asBase64.</summary>
    public const string Data = "data";
    public const string DataAsText = "data:asText";
    public const string DataAsBase64 = "data:asBase64";
    public const string Size = "size";

    /// <summary>A digest over the selected range, e.g. Digest("sha-256") → "digest:sha-256".</summary>
    public static string Digest(string algorithm) => "digest:" + algorithm;
}

/// <summary>Blob/get response objects have no state; blobs are immutable.</summary>
public sealed record BlobGetResponse
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<BlobData> List { get; init; }
    public IReadOnlyList<string> NotFound { get; init; } = [];
}

public sealed record BlobData
{
    public string? Id { get; init; }

    /// <summary>Null when the selected octets are not valid UTF-8 (see isEncodingProblem).</summary>
    [JsonPropertyName("data:asText")]
    public string? DataAsText { get; init; }

    [JsonPropertyName("data:asBase64")]
    public string? DataAsBase64 { get; init; }

    public bool IsEncodingProblem { get; init; }
    public bool IsTruncated { get; init; }

    /// <summary>Size of the whole blob, regardless of the requested range.</summary>
    public long? Size { get; init; }

    /// <summary>Catches the algorithm-named "digest:*" properties; read via <see cref="Digest"/>.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    /// <summary>The base64 "digest:{algorithm}" value, when it was requested.</summary>
    public string? Digest(string algorithm)
        => AdditionalProperties is not null
           && AdditionalProperties.TryGetValue("digest:" + algorithm, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

/// <summary>Blob/lookup (RFC 9404 §4.3): which objects of the given types reference each blob.</summary>
public sealed record BlobLookupArguments
{
    public required string AccountId { get; init; }
    /// <summary>Type names that can reference blobs (e.g. "Email", "Mailbox", "Thread"); the
    /// capability defining each must be in the request's using list.</summary>
    public required IReadOnlyList<string> TypeNames { get; init; }
    public required IReadOnlyList<string> Ids { get; init; }
}

public sealed record BlobLookupResponse
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<BlobLookupInfo> List { get; init; }
    public IReadOnlyList<string>? NotFound { get; init; }
}

public sealed record BlobLookupInfo
{
    /// <summary>The blob id that was looked up.</summary>
    public required string Id { get; init; }
    /// <summary>Type name → ids of that type referencing the blob (empty when none visible).</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> MatchedIds { get; init; }
}
