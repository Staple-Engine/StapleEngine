using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staple;

public class JsonStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(T) == typeToConvert;
    }

    public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString();

        if (Enum.TryParse<T>(input, true, out var outValue))
        {
            return outValue;
        }

        return default;
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var input = reader.GetString();

        if(Enum.TryParse<T>(input, true, out var outValue))
        {
            return outValue;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
