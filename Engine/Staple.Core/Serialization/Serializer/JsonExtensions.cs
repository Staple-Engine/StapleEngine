using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.Internal;

public static class JsonExtensions
{
    public static object GetRawValue(this JsonElement element)
    {
        switch(element.ValueKind)
        {
            case JsonValueKind.String:

                return element.GetString();

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:

                return null;

            case JsonValueKind.Object:

                {
                    var outValue = new Dictionary<string, object>();

                    foreach(var p in element.EnumerateObject())
                    {
                        outValue.Add(p.Name, p.Value.GetRawValue());
                    }

                    return outValue;
                }

            case JsonValueKind.Array:

                {
                    var outValue = new object[element.GetArrayLength()];

                    for(var i = 0; i < element.GetArrayLength(); i++)
                    {
                        outValue[i] = element[i].GetRawValue();
                    }

                    return outValue;
                }

            case JsonValueKind.True:

                return true;

            case JsonValueKind.False:

                return false;

            case JsonValueKind.Number:

                {
                    return element.TryGetByte(out var b) ? b :
                        element.TryGetSByte(out var sb) ? sb :
                        element.TryGetUInt16(out var us) ? us :
                        element.TryGetInt16(out var s) ? s :
                        element.TryGetUInt32(out var ui) ? ui :
                        element.TryGetInt32(out var i) ? i :
                        element.TryGetUInt64(out var ul) ? ul :
                        element.TryGetInt64(out var l) ? l :
                        element.TryGetSingle(out var f) ? f :
                        element.TryGetDouble(out var d) ? d :
                        null;
                }

            default:

                return null;
        }
    }

    public static T GetNumberValue<T>(this JsonElement element)
    {
        if(element.ValueKind != JsonValueKind.Number)
        {
            return default;
        }

        if(element.TryGetByte(out var b))
        {
            return (T)Convert.ChangeType(b, typeof(T));
        }

        if (element.TryGetSByte(out var sb))
        {
            return (T)Convert.ChangeType(sb, typeof(T));
        }

        if (element.TryGetUInt16(out var us))
        {
            return (T)Convert.ChangeType(us, typeof(T));
        }

        if (element.TryGetInt16(out var s))
        {
            return (T)Convert.ChangeType(s, typeof(T));
        }

        if (element.TryGetUInt32(out var ui))
        {
            return (T)Convert.ChangeType(ui, typeof(T));
        }

        if (element.TryGetInt32(out var i))
        {
            return (T)Convert.ChangeType(i, typeof(T));
        }

        if (element.TryGetUInt64(out var ul))
        {
            return (T)Convert.ChangeType(ul, typeof(T));
        }

        if (element.TryGetInt64(out var l))
        {
            return (T)Convert.ChangeType(l, typeof(T));
        }

        if (element.TryGetSingle(out var si))
        {
            return (T)Convert.ChangeType(si, typeof(T));
        }

        if (element.TryGetDouble(out var d))
        {
            return (T)Convert.ChangeType(d, typeof(T));
        }

        return default;
    }
}
