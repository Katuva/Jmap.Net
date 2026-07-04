namespace Jmap.Mail;

/// <summary>Every method name RFC 8621 defines, plus the MDN extension (RFC 9007), for
/// building <see cref="Invocation"/>s.</summary>
public static class MailMethods
{
    public const string MailboxGet = "Mailbox/get";
    public const string MailboxChanges = "Mailbox/changes";
    public const string MailboxQuery = "Mailbox/query";
    public const string MailboxQueryChanges = "Mailbox/queryChanges";
    public const string MailboxSet = "Mailbox/set";

    public const string ThreadGet = "Thread/get";
    public const string ThreadChanges = "Thread/changes";

    public const string EmailGet = "Email/get";
    public const string EmailChanges = "Email/changes";
    public const string EmailQuery = "Email/query";
    public const string EmailQueryChanges = "Email/queryChanges";
    public const string EmailSet = "Email/set";
    public const string EmailCopy = "Email/copy";
    public const string EmailImport = "Email/import";
    public const string EmailParse = "Email/parse";

    public const string SearchSnippetGet = "SearchSnippet/get";

    public const string IdentityGet = "Identity/get";
    public const string IdentityChanges = "Identity/changes";
    public const string IdentitySet = "Identity/set";

    public const string EmailSubmissionGet = "EmailSubmission/get";
    public const string EmailSubmissionChanges = "EmailSubmission/changes";
    public const string EmailSubmissionQuery = "EmailSubmission/query";
    public const string EmailSubmissionQueryChanges = "EmailSubmission/queryChanges";
    public const string EmailSubmissionSet = "EmailSubmission/set";

    public const string VacationResponseGet = "VacationResponse/get";
    public const string VacationResponseSet = "VacationResponse/set";

    // RFC 9007 (urn:ietf:params:jmap:mdn)
    public const string MdnSend = "MDN/send";
    public const string MdnParse = "MDN/parse";
}

// ── Method shapes that extend the RFC 8620 standards with mail-specific arguments ──────────

/// <summary>Mailbox/set (RFC 8621 §2.5): adds onDestroyRemoveEmails.</summary>
public sealed record MailboxSetArguments : SetArguments<Mailbox>
{
    /// <summary>When true, destroying a non-empty mailbox deletes its sole-homed emails
    /// instead of failing with "mailboxHasEmail".</summary>
    public bool OnDestroyRemoveEmails { get; init; }
}

/// <summary>Mailbox/changes (RFC 8621 §2.2) may name the only changed properties (e.g. counts).</summary>
public sealed record MailboxChangesResponse : ChangesResponse
{
    public IReadOnlyList<string>? UpdatedProperties { get; init; }
}

/// <summary>Mailbox/query (RFC 8621 §2.3): adds tree-aware sorting/filtering.</summary>
public sealed record MailboxQueryArguments : QueryArguments
{
    public bool SortAsTree { get; init; }
    public bool FilterAsTree { get; init; }
}

/// <summary>Email/get (RFC 8621 §4.2): adds body-fetch controls.</summary>
public sealed record EmailGetArguments : GetArguments
{
    public IReadOnlyList<string>? BodyProperties { get; init; }
    public bool FetchTextBodyValues { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("fetchHTMLBodyValues")]
    public bool FetchHtmlBodyValues { get; init; }
    public bool FetchAllBodyValues { get; init; }
    public long MaxBodyValueBytes { get; init; }
}

/// <summary>Email/query (RFC 8621 §4.4): adds collapseThreads.</summary>
public sealed record EmailQueryArguments : QueryArguments
{
    public bool CollapseThreads { get; init; }
}

/// <summary>Email/queryChanges (RFC 8621 §4.5): adds collapseThreads.</summary>
public sealed record EmailQueryChangesArguments : QueryChangesArguments
{
    public bool CollapseThreads { get; init; }
}

/// <summary>Email/import (RFC 8621 §4.8): file existing RFC 5322 blobs as emails.</summary>
public sealed record EmailImportArguments
{
    public required string AccountId { get; init; }
    public string? IfInState { get; init; }
    /// <summary>Creation-id → import instruction.</summary>
    public required IReadOnlyDictionary<string, EmailImport> Emails { get; init; }
}

public sealed record EmailImport
{
    public required string BlobId { get; init; }
    public required IReadOnlyDictionary<string, bool> MailboxIds { get; init; }
    public IReadOnlyDictionary<string, bool>? Keywords { get; init; }
    [System.Text.Json.Serialization.JsonConverter(typeof(Json.UtcDateConverter))]
    public DateTimeOffset? ReceivedAt { get; init; }
}

public sealed record EmailImportResponse
{
    public required string AccountId { get; init; }
    public string? OldState { get; init; }
    public required string NewState { get; init; }
    public IReadOnlyDictionary<string, Email>? Created { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCreated { get; init; }
}

/// <summary>Email/parse (RFC 8621 §4.9): parse message/rfc822 blobs (e.g. attached emails).</summary>
public sealed record EmailParseArguments
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<string> BlobIds { get; init; }
    public IReadOnlyList<string>? Properties { get; init; }
    public IReadOnlyList<string>? BodyProperties { get; init; }
    public bool FetchTextBodyValues { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("fetchHTMLBodyValues")]
    public bool FetchHtmlBodyValues { get; init; }
    public bool FetchAllBodyValues { get; init; }
    public long MaxBodyValueBytes { get; init; }
}

public sealed record EmailParseResponse
{
    public required string AccountId { get; init; }
    public IReadOnlyDictionary<string, Email>? Parsed { get; init; }
    public IReadOnlyList<string>? NotParsable { get; init; }
    public IReadOnlyList<string>? NotFound { get; init; }
}

/// <summary>SearchSnippet/get (RFC 8621 §5).</summary>
public sealed record SearchSnippetGetArguments
{
    public required string AccountId { get; init; }
    public JmapFilter? Filter { get; init; }
    public required IReadOnlyList<string> EmailIds { get; init; }
}

public sealed record SearchSnippetGetResponse
{
    public required string AccountId { get; init; }
    public required IReadOnlyList<SearchSnippet> List { get; init; }
    public IReadOnlyList<string>? NotFound { get; init; }
}

/// <summary>EmailSubmission/set (RFC 8621 §7.5): adds the on-success email updates —
/// e.g. move the sent message out of Drafts and set $sent keywords atomically.</summary>
public sealed record EmailSubmissionSetArguments : SetArguments<EmailSubmission>
{
    /// <summary>EmailSubmission id (or #creation-id) → patch applied to its email on success.</summary>
    public IReadOnlyDictionary<string, PatchObject>? OnSuccessUpdateEmail { get; init; }

    public IReadOnlyList<string>? OnSuccessDestroyEmail { get; init; }
}
