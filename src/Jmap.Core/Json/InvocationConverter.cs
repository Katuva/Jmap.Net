using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Jmap.Json;

/// <summary>Reads/writes an <see cref="Invocation"/> as its wire form: [name, arguments, callId].</summary>
public sealed class InvocationConverter : JsonConverter<Invocation>
{
    public override Invocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("An invocation must be a three-element array.");
        }

        reader.Read();
        var name = reader.GetString() ?? throw new JsonException("Invocation name must be a string.");
        reader.Read();
        var arguments = JsonNode.Parse(ref reader) as JsonObject ?? throw new JsonException("Invocation arguments must be an object.");
        reader.Read();
        var callId = reader.GetString() ?? throw new JsonException("Invocation call id must be a string.");
        reader.Read(); // EndArray

        return new Invocation(name, arguments, callId);
    }

    public override void Write(Utf8JsonWriter writer, Invocation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        value.Arguments.WriteTo(writer, options);
        writer.WriteStringValue(value.CallId);
        writer.WriteEndArray();
    }
}
