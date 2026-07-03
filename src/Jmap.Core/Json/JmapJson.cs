using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jmap.Json;

/// <summary>The serializer configuration every JMAP type is written and read with.</summary>
public static class JmapJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new InvocationConverter());
        options.Converters.Add(new JmapFilterConverter());
        return options;
    }
}
