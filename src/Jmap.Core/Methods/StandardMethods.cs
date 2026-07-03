using System.Text.Json;

namespace Jmap;

// The standard /get, /set, /changes, /query, /queryChanges and /copy shapes every JMAP data
// type shares (RFC 8620 §5). Type-specific methods extend these records with extra members.

public record GetArguments
{
    public required string AccountId { get; init; }

    /// <summary>Null fetches all objects of the type (when the server allows it).</summary>
    public IReadOnlyList<string>? Ids { get; init; }

    /// <summary>Null fetches all properties.</summary>
    public IReadOnlyList<string>? Properties { get; init; }
}

public sealed record GetResponse<T>
{
    public required string AccountId { get; init; }
    public required string State { get; init; }
    public required IReadOnlyList<T> List { get; init; }
    public IReadOnlyList<string> NotFound { get; init; } = [];
}

public record SetArguments<T>
{
    public required string AccountId { get; init; }

    /// <summary>Server rejects the whole call with "stateMismatch" if its state differs.</summary>
    public string? IfInState { get; init; }

    /// <summary>Creation-id → object to create; created ids are referenced elsewhere as "#creationId".</summary>
    public IReadOnlyDictionary<string, T>? Create { get; init; }

    /// <summary>Id → patch (JSON-pointer-ish paths, RFC 8620 §5.3).</summary>
    public IReadOnlyDictionary<string, PatchObject>? Update { get; init; }

    public IReadOnlyList<string>? Destroy { get; init; }
}

public sealed record SetResponse<T>
{
    public required string AccountId { get; init; }
    public string? OldState { get; init; }
    public required string NewState { get; init; }
    public IReadOnlyDictionary<string, T>? Created { get; init; }
    /// <summary>Id → server-set property changes, or null when nothing beyond the patch changed.</summary>
    public IReadOnlyDictionary<string, JsonElement?>? Updated { get; init; }
    public IReadOnlyList<string>? Destroyed { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCreated { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotUpdated { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotDestroyed { get; init; }
}

/// <summary>Why one create/update/destroy in a /set failed (RFC 8620 §5.3).</summary>
public sealed record SetError
{
    public required string Type { get; init; }
    public string? Description { get; init; }
    /// <summary>For "invalidProperties": which properties were at fault.</summary>
    public IReadOnlyList<string>? Properties { get; init; }
}

public record ChangesArguments
{
    public required string AccountId { get; init; }
    public required string SinceState { get; init; }
    public long? MaxChanges { get; init; }
}

public record ChangesResponse
{
    public required string AccountId { get; init; }
    public required string OldState { get; init; }
    public required string NewState { get; init; }
    public bool HasMoreChanges { get; init; }
    public IReadOnlyList<string> Created { get; init; } = [];
    public IReadOnlyList<string> Updated { get; init; } = [];
    public IReadOnlyList<string> Destroyed { get; init; } = [];
}

public record QueryArguments
{
    public required string AccountId { get; init; }
    public JmapFilter? Filter { get; init; }
    public IReadOnlyList<Comparator>? Sort { get; init; }
    public long Position { get; init; }
    public string? Anchor { get; init; }
    public long AnchorOffset { get; init; }
    public long? Limit { get; init; }
    public bool CalculateTotal { get; init; }
}

public record QueryResponse
{
    public required string AccountId { get; init; }
    public required string QueryState { get; init; }
    public bool CanCalculateChanges { get; init; }
    public required long Position { get; init; }
    public required IReadOnlyList<string> Ids { get; init; }
    public long? Total { get; init; }
    public long? Limit { get; init; }
}

public record QueryChangesArguments
{
    public required string AccountId { get; init; }
    public JmapFilter? Filter { get; init; }
    public IReadOnlyList<Comparator>? Sort { get; init; }
    public required string SinceQueryState { get; init; }
    public long? MaxChanges { get; init; }
    public string? UpToId { get; init; }
    public bool CalculateTotal { get; init; }
}

public record QueryChangesResponse
{
    public required string AccountId { get; init; }
    public required string OldQueryState { get; init; }
    public required string NewQueryState { get; init; }
    public long? Total { get; init; }
    public IReadOnlyList<string> Removed { get; init; } = [];
    public IReadOnlyList<AddedItem> Added { get; init; } = [];
}

public sealed record AddedItem(string Id, long Index);

public record CopyArguments<T>
{
    public required string FromAccountId { get; init; }
    public string? IfFromInState { get; init; }
    public required string AccountId { get; init; }
    public string? IfInState { get; init; }
    /// <summary>Creation-id → object whose <c>id</c> names the object to copy.</summary>
    public required IReadOnlyDictionary<string, T> Create { get; init; }
    public bool OnSuccessDestroyOriginal { get; init; }
    public string? DestroyFromIfInState { get; init; }
}

public sealed record CopyResponse<T>
{
    public required string FromAccountId { get; init; }
    public required string AccountId { get; init; }
    public string? OldState { get; init; }
    public required string NewState { get; init; }
    public IReadOnlyDictionary<string, T>? Created { get; init; }
    public IReadOnlyDictionary<string, SetError>? NotCreated { get; init; }
}
