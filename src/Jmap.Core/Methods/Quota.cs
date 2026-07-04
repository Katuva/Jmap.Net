namespace Jmap;

/// <summary>
/// A usage limit on an account (RFC 9425, urn:ietf:params:jmap:quota). Read-only — only
/// /get, /changes, /query and /queryChanges exist; both capability objects are empty.
/// </summary>
public sealed record Quota
{
    public string? Id { get; init; }
    /// <summary>What is being counted; see <see cref="QuotaResourceTypes"/>.</summary>
    public string? ResourceType { get; init; }
    public long Used { get; init; }
    /// <summary>Creates and updates fail once usage reaches this.</summary>
    public long HardLimit { get; init; }
    /// <summary>Who shares the quota; see <see cref="QuotaScopes"/>.</summary>
    public string? Scope { get; init; }
    public string? Name { get; init; }
    /// <summary>Data type names counted against this quota (e.g. "Mail").</summary>
    public IReadOnlyList<string> Types { get; init; } = [];
    public long? WarnLimit { get; init; }
    public long? SoftLimit { get; init; }
    public string? Description { get; init; }
}

public static class QuotaResourceTypes
{
    public const string Count = "count";
    public const string Octets = "octets";
}

public static class QuotaScopes
{
    public const string Account = "account";
    public const string Domain = "domain";
    public const string Global = "global";
}

/// <summary>Quota/changes (RFC 9425 §4.3) may name the only changed properties (usually
/// ["used"]), for back-referencing into a Quota/get properties argument.</summary>
public sealed record QuotaChangesResponse : ChangesResponse
{
    public IReadOnlyList<string>? UpdatedProperties { get; init; }
}

/// <summary>A Quota/query filter condition (RFC 9425 §4.4); sortable by "name" and "used".</summary>
public sealed record QuotaFilterCondition : JmapFilter
{
    /// <summary>Substring match against the quota's name.</summary>
    public string? Name { get; init; }
    public string? Scope { get; init; }
    public string? ResourceType { get; init; }
    /// <summary>Matches quotas whose types list contains this type name.</summary>
    public string? Type { get; init; }
}
