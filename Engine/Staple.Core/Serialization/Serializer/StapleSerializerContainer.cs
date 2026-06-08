using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.Internal;

internal sealed class StapleSerializerContainer
{
    public Dictionary<string, StapleSerializerField> fields = [];
    public string typeName;

    public SerializableStapleAssetContainer ToSerializableContainer()
    {
        var outValue = new SerializableStapleAssetContainer()
        {
            typeName = typeName,
        };

        foreach(var pair in fields)
        {
            outValue.fields.Add(pair.Key, pair.Value.ToSerializableParameter());
        }

        return outValue;
    }

    public Dictionary<string, object> ToRawContainer()
    {
        var outValue = new Dictionary<string, object>();

        foreach(var pair in fields)
        {
            outValue.Add(pair.Key, pair.Value.ToRawValue());
        }

        return outValue;
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(List<uint>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(StapleSerializerContainer))]
[JsonSerializable(typeof(Dictionary<string, StapleSerializerField>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(StapleSerializerField))]
[JsonSerializable(typeof(Vector2Holder))]
[JsonSerializable(typeof(Vector3Holder))]
[JsonSerializable(typeof(Vector4Holder))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
internal partial class StapleSerializerContainerSerializationContext : JsonSerializerContext
{
}

