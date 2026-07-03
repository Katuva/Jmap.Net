using System.Text.Json;
using System.Text.Json.Serialization;
using Jmap.Json;

namespace Jmap;

/// <summary>
/// The JMAP session resource (RFC 8620 §2): what the server supports, the accounts the
/// credentials can access, and the URL (templates) for the API, blobs and push.
/// </summary>
public sealed record JmapSession
{
    /// <summary>Raw capability objects keyed by URN; use <see cref="TryGetCapability{T}"/> for typed access.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Capabilities { get; init; }

    public required IReadOnlyDictionary<string, JmapAccount> Accounts { get; init; }

    /// <summary>Per capability URN, the account to use when the client has no other preference.</summary>
    public IReadOnlyDictionary<string, string> PrimaryAccounts { get; init; } = new Dictionary<string, string>();

    public required string Username { get; init; }

    public required Uri ApiUrl { get; init; }

    /// <summary>URI template with {accountId}, {blobId}, {type} and {name} variables.</summary>
    public required string DownloadUrl { get; init; }

    /// <summary>URI template with an {accountId} variable.</summary>
    public required string UploadUrl { get; init; }

    /// <summary>URI template with {types}, {closeafter} and {ping} variables.</summary>
    public required string EventSourceUrl { get; init; }

    /// <summary>Changes whenever anything in the session changes; re-fetch the session when it moves.</summary>
    public required string State { get; init; }

    public bool TryGetCapability<T>(string urn, out T? capability)
    {
        if (Capabilities.TryGetValue(urn, out var element))
        {
            capability = element.Deserialize<T>(JmapJson.Options);
            return capability is not null;
        }

        capability = default;
        return false;
    }

    /// <summary>The primary account id for a capability, or the sole account as a fallback.</summary>
    public string PrimaryAccountId(string capabilityUrn = JmapCapabilities.Core)
        => PrimaryAccounts.TryGetValue(capabilityUrn, out var id)
            ? id
            : Accounts.Count == 1
                ? Accounts.Keys.First()
                : throw new JmapException($"No primary account for '{capabilityUrn}' and more than one account exists.");
}

/// <summary>One account visible through the session (RFC 8620 §1.6.2).</summary>
public sealed record JmapAccount
{
    public required string Name { get; init; }

    public bool IsPersonal { get; init; }

    public bool IsReadOnly { get; init; }

    [JsonPropertyName("accountCapabilities")]
    public IReadOnlyDictionary<string, JsonElement> AccountCapabilities { get; init; } = new Dictionary<string, JsonElement>();
}

/// <summary>The core capability object (RFC 8620 §2, urn:ietf:params:jmap:core).</summary>
public sealed record CoreCapability
{
    public long MaxSizeUpload { get; init; }
    public long MaxConcurrentUpload { get; init; }
    public long MaxSizeRequest { get; init; }
    public long MaxConcurrentRequests { get; init; }
    public long MaxCallsInRequest { get; init; }
    public long MaxObjectsInGet { get; init; }
    public long MaxObjectsInSet { get; init; }
    public IReadOnlyList<string> CollationAlgorithms { get; init; } = [];
}
