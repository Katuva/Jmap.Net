using System.Text.Json;
using System.Text.Json.Nodes;
using Jmap.Json;

namespace Jmap;

/// <summary>
/// One method call or response (RFC 8620 §3.2): on the wire, a three-element array of
/// [name, arguments, method-call-id]. Arguments are held as JSON so back-references
/// ("#argument") can be mixed with typed values.
/// </summary>
public sealed class Invocation
{
    public Invocation(string name, JsonObject arguments, string callId)
    {
        Name = name;
        Arguments = arguments;
        CallId = callId;
    }

    public string Name { get; }

    public JsonObject Arguments { get; }

    public string CallId { get; }

    /// <summary>True for a method-level error response (RFC 8620 §3.6.2).</summary>
    public bool IsError => Name == "error";

    public static Invocation Create<TArguments>(string name, TArguments arguments, string callId)
    {
        var node = JsonSerializer.SerializeToNode(arguments, JmapJson.Options) as JsonObject
            ?? throw new ArgumentException("Method arguments must serialise to a JSON object.", nameof(arguments));
        return new Invocation(name, node, callId);
    }

    /// <summary>
    /// Replaces a literal argument with a back-reference to an earlier call's result
    /// (RFC 8620 §3.7): the argument is sent as <c>"#name": {resultOf, name, path}</c>.
    /// </summary>
    public Invocation WithBackReference(string argument, ResultReference reference)
    {
        Arguments.Remove(argument);
        Arguments["#" + argument] = JsonSerializer.SerializeToNode(reference, JmapJson.Options);
        return this;
    }

    public T ArgumentsAs<T>()
        => Arguments.Deserialize<T>(JmapJson.Options)
           ?? throw new JmapException($"Could not read '{Name}' arguments as {typeof(T).Name}.");

    public JmapMethodError AsError()
        => IsError ? ArgumentsAs<JmapMethodError>() : throw new InvalidOperationException($"'{Name}' is not an error invocation.");
}

/// <summary>A reference to part of an earlier method call's result (RFC 8620 §3.7).</summary>
public sealed record ResultReference(string ResultOf, string Name, string Path);
