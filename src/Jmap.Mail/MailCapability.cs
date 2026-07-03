namespace Jmap.Mail;

/// <summary>The mail capability object (RFC 8621 §1.3, urn:ietf:params:jmap:mail),
/// found per account in <c>accountCapabilities</c>.</summary>
public sealed record MailCapability
{
    public long? MaxMailboxesPerEmail { get; init; }
    public long? MaxMailboxDepth { get; init; }
    public long MaxSizeMailboxName { get; init; }
    public long MaxSizeAttachmentsPerEmail { get; init; }
    public IReadOnlyList<string> EmailQuerySortOptions { get; init; } = [];
    public bool MayCreateTopLevelMailbox { get; init; }
}

/// <summary>The submission capability object (RFC 8621 §1.3, urn:ietf:params:jmap:submission).</summary>
public sealed record SubmissionCapability
{
    /// <summary>Seconds a submission may be undone after sending (0 = no undo window).</summary>
    public long MaxDelayedSend { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>?> SubmissionExtensions { get; init; }
        = new Dictionary<string, IReadOnlyList<string>?>();
}
