using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Staple.Tooling;

internal class FloatConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(float) ||
            objectType == typeof(double);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (objectType == typeof(float))
        {
            return reader.ReadAsDecimal();
        }
        else if (objectType == typeof(double))
        {
            return reader.ReadAsDouble();
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if(value is float f)
        {
            writer.WriteRawValue(f.ToString("0.0###", CultureInfo.InvariantCulture));
        }
        else if(value is double d)
        {
            writer.WriteRawValue(d.ToString("0.0###", CultureInfo.InvariantCulture));
        }
    }
}
