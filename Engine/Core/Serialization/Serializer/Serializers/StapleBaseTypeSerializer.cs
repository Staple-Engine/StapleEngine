using System;
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
            _ => false
        };
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo)
    {
        return fieldInfo.value;
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, JsonElement element)
    {
        return fieldType switch
        {
            Type t when t == typeof(string) && element.ValueKind == JsonValueKind.String => element.GetString(),
            Type t when t == typeof(byte) && element.ValueKind == JsonValueKind.Number => element.GetByte(),
            Type t when t == typeof(sbyte) && element.ValueKind == JsonValueKind.Number => element.GetSByte(),
            Type t when t == typeof(ushort) && element.ValueKind == JsonValueKind.Number => element.GetUInt16(),
            Type t when t == typeof(short) && element.ValueKind == JsonValueKind.Number => element.GetInt16(),
            Type t when t == typeof(uint) && element.ValueKind == JsonValueKind.Number => element.GetUInt32(),
            Type t when t == typeof(int) && element.ValueKind == JsonValueKind.Number => element.GetInt32(),
            Type t when t == typeof(float) && element.ValueKind == JsonValueKind.Number => element.GetSingle(),
            Type t when t == typeof(double) && element.ValueKind == JsonValueKind.Number => element.GetDouble(),
            Type t when t == typeof(bool) && (element.ValueKind == JsonValueKind.True ||
                element.ValueKind == JsonValueKind.False) => element.GetBoolean(),
            _ => null,
        };
    }

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        return instance;
    }

    public object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        return instance;
    }
}
