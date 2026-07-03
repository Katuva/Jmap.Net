using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jmap.Json;

/// <summary>
/// RFC 8620 <c>UTCDate</c>: a date-time that must be serialised in UTC with a "Z" suffix
/// (e.g. <c>2026-07-03T14:12:00Z</c>). Apply per property with <see cref="JsonConverterAttribute"/>.
/// </summary>
public sealed class UtcDateConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
}
