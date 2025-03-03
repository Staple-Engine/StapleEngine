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
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(StapleSerializerContainer))]
[JsonSerializable(typeof(Dictionary<string, StapleSerializerField>))]
[JsonSerializable(typeof(StapleSerializerField))]
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

