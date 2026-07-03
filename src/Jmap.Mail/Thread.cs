namespace Jmap.Mail;

/// <summary>A conversation: the server-computed, date-ordered email ids (RFC 8621 §3).</summary>
public sealed record Thread(string Id, IReadOnlyList<string> EmailIds);

/// <summary>A search-match preview with the matching terms marked (RFC 8621 §5).</summary>
public sealed record SearchSnippet
{
    public required string EmailId { get; init; }
    /// <summary>Subject with matches wrapped in &lt;mark&gt; tags; null when the subject didn't match.</summary>
    public string? Subject { get; init; }
    /// <summary>Body extract with matches wrapped in &lt;mark&gt; tags; null when the body didn't match.</summary>
    public string? Preview { get; init; }
}
