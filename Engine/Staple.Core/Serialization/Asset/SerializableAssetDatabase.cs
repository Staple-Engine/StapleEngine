using MessagePack;
using System.Collections.Generic;

namespace Staple.Internal;

[MessagePackObject]
public class SerializableAssetDatabaseHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader =
        [
            'S', 'A', 'D', 'B'
        ];

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializableAssetDatabaseAssetInfo
{
    [Key(0)]
    public string guid;

    [Key(1)]
    public string name;

    [Key(2)]
    public string typeName;

    [Key(3)]
    public string path;

    [Key(4)]
    public long lastModified;

    public override string ToString()
    {
        return $"{guid} {name} {typeName}";
    }
}

[MessagePackObject]
public class SerializableAssetDatabase
{
    [Key(0)]
    public Dictionary<string, List<SerializableAssetDatabaseAssetInfo>> assets = [];
}
