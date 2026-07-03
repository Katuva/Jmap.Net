namespace Jmap;

/// <summary>
/// A /query filter (RFC 8620 §5.5): either a <see cref="FilterOperator"/> combining
/// sub-filters, or a data-type-specific condition record deriving from this type
/// (e.g. Jmap.Mail's EmailFilterCondition).
/// </summary>
public abstract record JmapFilter
{
    public static FilterOperator And(params JmapFilter[] conditions) => new("AND", conditions);

    public static FilterOperator Or(params JmapFilter[] conditions) => new("OR", conditions);

    public static FilterOperator Not(params JmapFilter[] conditions) => new("NOT", conditions);
}

public sealed record FilterOperator(string Operator, IReadOnlyList<JmapFilter> Conditions) : JmapFilter;

/// <summary>A /query sort key (RFC 8620 §5.5).</summary>
public sealed record Comparator(string Property, bool IsAscending = true, string? Collation = null);
