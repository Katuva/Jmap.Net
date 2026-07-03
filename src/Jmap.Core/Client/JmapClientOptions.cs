using System.Net.Http.Headers;
using System.Text;

namespace Jmap;

/// <summary>How to reach and authenticate with a JMAP server.</summary>
public sealed record JmapClientOptions
{
    /// <summary>The session resource URL, e.g. <c>https://api.fastmail.com/jmap/session</c>.
    /// RFC 8620 §2.2 also defines discovery at <c>https://{domain}/.well-known/jmap</c>.</summary>
    public required Uri SessionUrl { get; init; }

    /// <summary>Bearer-token authentication (the common case for JMAP providers).</summary>
    public string? BearerToken { get; init; }

    /// <summary>HTTP Basic authentication, for servers that support it.</summary>
    public (string Username, string Password)? BasicCredentials { get; init; }

    internal AuthenticationHeaderValue? BuildAuthorization() => this switch
    {
        { BearerToken: { } token } => new AuthenticationHeaderValue("Bearer", token),
        { BasicCredentials: ({ } user, { } password) } => new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"))),
        _ => null,
    };
}

/// <summary>The result of a blob upload (RFC 8620 §6.1).</summary>
public sealed record BlobInfo
{
    public required string AccountId { get; init; }
    public required string BlobId { get; init; }
    public required string Type { get; init; }
    public required long Size { get; init; }
}
