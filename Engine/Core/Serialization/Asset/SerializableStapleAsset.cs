using MessagePack;
using System.Collections.Generic;

namespace Staple.Internal;

[MessagePackObject]
public class SerializableStapleAssetHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader =
        [
            'S', 'S', 'A', 'S'
        ];

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializableStapleAssetParameter
{
    [Key(0)]
    public string typeName;

    [Key(1)]
    public object value;

    internal StapleSerializerField ToSerializerField()
    {
        return new()
        {
            typeName = typeName,
            value = value is SerializableStapleAssetContainer container ? container.ToSerializerContainer() : value,
        };
    }
}

[MessagePackObject]
public class SerializableStapleAssetContainer
{
    [Key(0)]
    public string typeName;

    [Key(1)]
    public Dictionary<string, SerializableStapleAssetParameter> parameters = [];

    internal StapleSerializerContainer ToSerializerContainer()
    {
        var outValue = new StapleSerializerContainer()
        {
            typeName = typeName,
        };

        foreach(var pair in parameters)
        {
            outValue.fields.Add(pair.Key, pair.Value.ToSerializerField());
        }

        return outValue;
    }
}

[MessagePackObject]
public class SerializableStapleAsset
{
    [Key(0)]
    public string guid;

    [Key(1)]
    public string typeName;

    [Key(2)]
    public Dictionary<string, SerializableStapleAssetParameter> parameters = [];

    internal StapleSerializerContainer ToSerializerContainer(out string guid)
    {
        guid = this.guid;

        var outValue = new StapleSerializerContainer()
        {
            typeName = typeName,
        };

        foreach (var pair in parameters)
        {
            outValue.fields.Add(pair.Key, pair.Value.ToSerializerField());
        }

        return outValue;
    }
}
