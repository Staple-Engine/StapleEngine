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
            Type t when t == typeof(float) => true,
            Type t when t == typeof(double) => true,
            Type t when t == typeof(bool) => true,
            Type t when t == typeof(Vector2) => true,
            Type t when t == typeof(Vector3) => true,
            Type t when t == typeof(Vector4) => true,
            Type t when t == typeof(Quaternion) => true,
            _ => false
        };
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => ((Vector2Holder)fieldInfo.value).ToVector2(),
            Type t when t == typeof(Vector3) => ((Vector3Holder)fieldInfo.value).ToVector3(),
            Type t when t == typeof(Vector4) => ((Vector4Holder)fieldInfo.value).ToVector4(),
            Type t when t == typeof(Quaternion) => ((Vector4Holder)fieldInfo.value).ToQuaternion(),
            _ => fieldInfo.value,
        };
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, JsonElement element)
    {
        switch (fieldType)
        {
            case Type t when t == typeof(string) && element.ValueKind == JsonValueKind.String:

                return element.GetString();

            case Type t when t == typeof(byte) && element.ValueKind == JsonValueKind.Number:

                return element.GetByte();

            case Type t when t == typeof(sbyte) && element.ValueKind == JsonValueKind.Number:

                return element.GetSByte();

            case Type t when t == typeof(ushort) && element.ValueKind == JsonValueKind.Number:

                return element.GetUInt16();

            case Type t when t == typeof(short) && element.ValueKind == JsonValueKind.Number:

                return element.GetInt16();

            case Type t when t == typeof(uint) && element.ValueKind == JsonValueKind.Number:

                return element.GetUInt32();

            case Type t when t == typeof(int) && element.ValueKind == JsonValueKind.Number:

                return element.GetInt32();

            case Type t when t == typeof(float) && element.ValueKind == JsonValueKind.Number:

                return element.GetSingle();

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
                        return new Vector2(xProp.GetSingle(), yProp.GetSingle());
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
                        return new Vector3(xProp.GetSingle(), yProp.GetSingle(), zProp.GetSingle());
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
                        return new Vector4(xProp.GetSingle(), yProp.GetSingle(), zProp.GetSingle(), wProp.GetSingle());
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
                        return new Quaternion(xProp.GetSingle(), yProp.GetSingle(), zProp.GetSingle(), wProp.GetSingle());
                    }
                }

                break;
        };

        return null;
    }

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => new Vector2Holder((Vector2)instance),
            Type t when t == typeof(Vector3) => new Vector3Holder((Vector3)instance),
            Type t when t == typeof(Vector4) => new Vector4Holder((Vector4)instance),
            Type t when t == typeof(Quaternion) => new Vector4Holder((Quaternion)instance),
            _ => instance,
        };
    }

    public object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        return fieldType switch
        {
            Type t when t == typeof(Vector2) => new Vector2Holder((Vector2)instance),
            Type t when t == typeof(Vector3) => new Vector3Holder((Vector3)instance),
            Type t when t == typeof(Vector4) => new Vector4Holder((Vector4)instance),
            Type t when t == typeof(Quaternion) => new Vector4Holder((Quaternion)instance),
            _ => instance,
        };
    }
}
