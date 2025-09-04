using MessagePack;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.Internal;

[MessagePackObject]
public class SceneListHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'S', 'C', 'L'
    };

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
[Serializable]
public class SceneList
{
    [Key(0)]
    public List<string> scenes = [];
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(SceneList))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
internal partial class SceneListSerializationContext : JsonSerializerContext
{
}
