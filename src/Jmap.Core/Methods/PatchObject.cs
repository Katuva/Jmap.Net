namespace Jmap;

/// <summary>
/// A /set update patch (RFC 8620 §5.3): paths (JSON-pointer-ish, "/"-separated) to new
/// values, e.g. <c>{"keywords/$seen": true}</c> or <c>{"mailboxIds/abc": null}</c>.
/// </summary>
public sealed class PatchObject : Dictionary<string, object?>
{
    public static PatchObject Set(string path, object? value) => new PatchObject().With(path, value);

    public PatchObject With(string path, object? value)
    {
        this[path] = value;
        return this;
    }

    /// <summary>Removes the value at a path (a null patch value deletes it).</summary>
    public PatchObject Without(string path) => With(path, null);
}
