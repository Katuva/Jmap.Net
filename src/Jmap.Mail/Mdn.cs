namespace Jmap.Mail;

/// <summary>
/// A message disposition notification, i.e. a read receipt (RFC 9007,
/// urn:ietf:params:jmap:mdn). Sent with MDN/send for a received email that carries a
/// Disposition-Notification-To header; MDN/parse decodes received ones (e.g. the blobs in
/// an EmailSubmission's mdnBlobIds). To request an MDN when sending, set the
/// "header:Disposition-Notification-To:asText" property on the draft Email instead.
/// </summary>
public sealed record Mdn
{
    /// <summary>The received email this MDN is about. Required for MDN/send; may be null
    /// from MDN/parse when the original message can't be identified.</summary>
    public string? ForEmailId { get; init; }

    public string? Subject { get; init; }

    /// <summary>The human-readable plain-text part of the MDN.</summary>
    public string? TextBody { get; init; }

    public bool IncludeOriginalMessage { get; init; }

    /// <summary>The MUA name for the report part; null may be better for privacy.</summary>
    public string? ReportingUA { get; init; }

    public MdnDisposition? Disposition { get; init; }

    /// <summary>Server-set: the gateway that translated a foreign notification, if any.</summary>
    public string? MdnGateway { get; init; }

    /// <summary>Server-set: the recipient address as specified by the original sender.</summary>
    public string? OriginalRecipient { get; init; }

    /// <summary>RFC 8098 address form, e.g. "rfc822; joe@example.com". Left null, the server
    /// derives it from the MDN/send identity.</summary>
    public string? FinalRecipient { get; init; }

    /// <summary>Server-set: the Message-ID header (not the JMAP id) of the original message.</summary>
    public string? OriginalMessageId { get; init; }

    /// <summary>Server-set: messages accompanying an "error" disposition modifier.</summary>
    public IReadOnlyList<string>? Error { get; init; }

    /// <summary>Extension-field name → value (RFC 8098 §3.3).</summary>
    public IReadOnlyDictionary<string, string>? ExtensionFields { get; init; }
}

/// <summary>How the message was disposed of (RFC 9007 §2). Values are the lowercase RFC 8098
/// tokens; see the Mdn*Modes/MdnDispositionTypes constants.</summary>
public sealed record MdnDisposition(string ActionMode, string SendingMode, string Type);

public static class MdnActionModes
{
    public const string ManualAction = "manual-action";
    public const string AutomaticAction = "automatic-action";
}

public static class MdnSendingModes
{
    public const string SentManually = "mdn-sent-manually";
    public const string SentAutomatically = "mdn-sent-automatically";
}

public static class MdnDispositionTypes
{
    public const string Deleted = "deleted";
    public const string Dispatched = "dispatched";
    public const string Displayed = "displayed";
    public const string Processed = "processed";
}

/// <summary>MDN/send (RFC 9007 §2.1). The request's using list must include both the mdn
/// and mail capability URNs, because sending implies an Email/set.</summary>
public sealed record MdnSendArguments
{
    public required string AccountId { get; init; }

    /// <summary>The identity the MDN is sent as; also determines finalRecipient.</summary>
    public required string IdentityId { get; init; }

    /// <summary>Creation-id → MDN to send.</summary>
    public required IReadOnlyDictionary<string, Mdn> Send { get; init; }

    /// <summary>"#creation-id" → patch applied to the email on success. The server insists
    /// the patch sets keywords/$mdnsent, and rejects re-sends with "mdnAlreadySent".</summary>
    public IReadOnlyDictionary<string, PatchObject>? OnSuccessUpdateEmail { get; init; }
}

public sealed record MdnSendResponse
{
    public required string AccountId { get; init; }

    /// <summary>Creation-id → the properties of each sent MDN the client didn't set itself.</summary>
    public IReadOnlyDictionary<string, Mdn>? Sent { get; init; }

    public IReadOnlyDictionary<string, SetError>? NotSent { get; init; }
}

/// <summary>MDN/parse (RFC 9007 §2.2): parse message/rfc822 blobs as MDNs.</summary>
public sealed record MdnParseArguments
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<string> BlobIds { get; init; }
}

public sealed record MdnParseResponse
{
    public required string AccountId { get; init; }
    public IReadOnlyDictionary<string, Mdn>? Parsed { get; init; }
    public IReadOnlyList<string>? NotParsable { get; init; }
    public IReadOnlyList<string>? NotFound { get; init; }
}
