using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jmap.Json;

/// <summary>
/// Serialises the runtime type of a <see cref="JmapFilter"/> (a client mostly writes filters).
/// Reading is intentionally unsupported: the concrete condition type depends on the data type
/// being queried, which the JSON alone doesn't identify.
/// </summary>
public sealed class JmapFilterConverter : JsonConverter<JmapFilter>
{
    public override JmapFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException(
            "Filters are written by clients, not read back; deserialise into the concrete condition type instead.");

    public override void Write(Utf8JsonWriter writer, JmapFilter value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
