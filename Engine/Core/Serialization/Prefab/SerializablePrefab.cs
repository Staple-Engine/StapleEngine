using MessagePack;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.Internal;

[MessagePackObject]
public class SerializablePrefabHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'P', 'R', 'E'
    };

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializablePrefab
{
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    public SceneObject mainObject;

    [Key(2)]
    public List<SceneObject> children = new();

    [Key(3)]
    public string typeName = typeof(Prefab).FullName;
}

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
[JsonSerializable(typeof(List<SceneObject>))]
[JsonSerializable(typeof(SceneObject))]
[JsonSerializable(typeof(SceneObjectTransform))]
[JsonSerializable(typeof(SerializablePrefab))]
internal partial class SerializablePrefabSerializationContext : JsonSerializerContext
{
}
