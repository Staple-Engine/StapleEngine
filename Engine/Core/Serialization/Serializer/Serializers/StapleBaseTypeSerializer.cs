using System;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

namespace Staple.Internal;

internal class StapleBaseTypeSerializer : IStapleTypeSerializer
{
    public bool HandlesType(Type type)
    {
        return type switch
        {
            Type t when t == typeof(string) => true,
            Type t when t == typeof(byte) => true,
            Type t when t == typeof(sbyte) => true,
            Type t when t == typeof(ushort) => true,
            Type t when t == typeof(short) => true,
            Type t when t == typeof(uint) => true,
            Type t when t == typeof(int) => true,
            Type t when t == typeof(ulong) => true,
            Type t when t == typeof(long) => true,
            Type t when t == typeof(float) => true,
            Type t when t == typeof(double) => true,
            Type t when t == typeof(bool) => true,
            Type t when t == typeof(Vector2) => true,
            Type t when t == typeof(Vector3) => true,
            Type t when t == typeof(Vector4) => true,
            Type t when t == typeof(Quaternion) => true,
            Type t when t.IsEnum => true,
            _ => false
        };
    }

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => new Vector2Holder((Vector2)instance),
            Type t when t == typeof(Vector3) => new Vector3Holder((Vector3)instance),
            Type t when t == typeof(Vector4) => new Vector4Holder((Vector4)instance),
            Type t when t == typeof(Quaternion) => new Vector4Holder((Quaternion)instance),
            Type t when t.IsEnum => t.GetCustomAttribute<FlagsAttribute>() != null ?
                (t.GetEnumUnderlyingType() == typeof(int) ? (long)(int)instance :
                t.GetEnumUnderlyingType() == typeof(uint) ? (long)(uint)instance :
                t.GetEnumUnderlyingType() == typeof(long) ? (long)instance :
                t.GetEnumUnderlyingType() == typeof(ulong) ? (long)(ulong)instance : 0) :
                instance.ToString(),
            _ => instance,
        };
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, StapleSerializationMode mode)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => ((Vector2Holder)fieldInfo.value).ToVector2(),
            Type t when t == typeof(Vector3) => ((Vector3Holder)fieldInfo.value).ToVector3(),
            Type t when t == typeof(Vector4) => ((Vector4Holder)fieldInfo.value).ToVector4(),
            Type t when t == typeof(Quaternion) => ((Vector4Holder)fieldInfo.value).ToQuaternion(),
            Type t when t.IsEnum => t.GetCustomAttribute<FlagsAttribute>() != null ?
                (t.GetEnumUnderlyingType() == typeof(int) ? Enum.ToObject(t, (long)(int)fieldInfo.value) :
                t.GetEnumUnderlyingType() == typeof(uint) ? Enum.ToObject(t, (long)(uint)fieldInfo.value) :
                t.GetEnumUnderlyingType() == typeof(long) ? Enum.ToObject(t, (long)fieldInfo.value) :
                t.GetEnumUnderlyingType() == typeof(ulong) ? Enum.ToObject(t, (long)(ulong)fieldInfo.value) : 0) :
                Enum.TryParse(t, (string)fieldInfo.value, true, out var e) ? e : null,
            _ => fieldInfo.value,
        };
    }

    public object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => new Vector2Holder((Vector2)instance),
            Type t when t == typeof(Vector3) => new Vector3Holder((Vector3)instance),
            Type t when t == typeof(Vector4) => new Vector4Holder((Vector4)instance),
            Type t when t == typeof(Quaternion) => new Vector4Holder((Quaternion)instance),
            Type t when t.IsEnum => t.GetCustomAttribute<FlagsAttribute>() != null ?
                (t.GetEnumUnderlyingType() == typeof(int) ? (long)(int)instance :
                t.GetEnumUnderlyingType() == typeof(uint) ? (long)(uint)instance :
                t.GetEnumUnderlyingType() == typeof(long) ? (long)instance :
                t.GetEnumUnderlyingType() == typeof(ulong) ? (long)(ulong)instance : 0) :
                instance.ToString(),
            _ => instance,
        };
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo,
        JsonElement element, StapleSerializationMode mode)
    {
        switch (fieldType)
        {
            case Type t when t == typeof(string) && element.ValueKind == JsonValueKind.String:

                return element.GetString();

            case Type t when t == typeof(byte) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<byte>();

            case Type t when t == typeof(sbyte) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<sbyte>();

            case Type t when t == typeof(ushort) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<ushort>();

            case Type t when t == typeof(short) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<short>();

            case Type t when t == typeof(uint) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<uint>();

            case Type t when t == typeof(int) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<int>();

            case Type t when t == typeof(ulong) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<ulong>();

            case Type t when t == typeof(long) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<long>();

            case Type t when t == typeof(float) && element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<float>();

            case Type t when t == typeof(double) && element.ValueKind == JsonValueKind.Number:

                return element.GetDouble();

            case Type t when t == typeof(bool) && (element.ValueKind == JsonValueKind.True ||
                element.ValueKind == JsonValueKind.False):

                return element.GetBoolean();

            case Type t when t == typeof(Vector2) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector2Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector2Holder.y), out var yProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector2(xProp.GetNumberValue<float>(),
                            yProp.GetNumberValue<float>());
                    }
                }

                break;

            case Type t when t == typeof(Vector3) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector3Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector3Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector3Holder.z), out var zProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector3(xProp.GetNumberValue<float>(),
                            yProp.GetNumberValue<float>(),
                            zProp.GetNumberValue<float>());
                    }
                }

                break;

            case Type t when t == typeof(Vector4) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector4Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.z), out var zProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.w), out var wProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number &&
                        wProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector4(xProp.GetNumberValue<float>(),
                            yProp.GetNumberValue<float>(),
                            zProp.GetNumberValue<float>(),
                            wProp.GetNumberValue<float>());
                    }
                }

                break;

            case Type t when t == typeof(Quaternion) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector4Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.z), out var zProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.w), out var wProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number &&
                        wProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Quaternion(xProp.GetNumberValue<float>(),
                            yProp.GetNumberValue<float>(),
                            zProp.GetNumberValue<float>(),
                            wProp.GetNumberValue<float>());
                    }
                }

                break;

            case Type t when t.IsEnum:

                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        if (Enum.TryParse(t, element.GetString(), true, out var e))
                        {
                            return e;
                        }
                    }
                    else
                    {
                        var value = element.GetNumberValue<long>();

                        return Enum.ToObject(t, value);
                    }
                }

                break;
        };

        return null;
    }
}
