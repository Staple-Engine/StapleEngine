using MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Staple.Internal;

[JsonConverter(typeof(JsonStringEnumConverter<SceneObjectKind>))]
public enum SceneObjectKind
{
    Entity
}

public enum SceneComponentParameterType
{
    Bool,
    Int,
    Float,
    String,
    Vector2,
    Vector3,
    Vector4,
    Array,
}

[Serializable]
[MessagePackObject]
public class SceneObjectTransform
{
    [Key(0)]
    public Vector3Holder position = new();

    [Key(1)]
    public Vector3Holder rotation = new();

    [Key(2)]
    public Vector3Holder scale = new();
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Vector3Holder))]
[JsonSerializable(typeof(SceneObjectTransform))]

internal partial class SceneObjectTransformSerializationContext : JsonSerializerContext
{
}

[Serializable]
[MessagePackObject]
public class SceneComponent
{
    [Key(0)]
    public string type;

    [Key(1)]
    [JsonIgnore]
    [NonSerialized]
    public Dictionary<string, object> parameters = [];

    [IgnoreMember]
    public Dictionary<string, object> data = [];
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Vector2Holder))]
[JsonSerializable(typeof(Vector3Holder))]
[JsonSerializable(typeof(Vector4Holder))]

internal partial class SceneComponentSerializationContext : JsonSerializerContext
{
}

[Serializable]
[MessagePackObject]
[DebuggerDisplay("SceneObject {name}")]
public class SceneObject
{
    [Key(0)]
    public SceneObjectKind kind;

    [Key(1)]
    public string name;

    [Key(2)]
    public int ID;

    [Key(3)]
    public int parent;

    [Key(4)]
    public SceneObjectTransform transform = new();

    [Key(5)]
    public List<SceneComponent> components = [];

    [Key(6)]
    public string layer;

    [Key(7)]
    public bool enabled;

    [Key(8)]
    public string prefabGuid;

    [Key(9)]
    public int prefabLocalID;
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(SceneObject))]
[JsonSerializable(typeof(List<SceneObject>))]
[JsonSerializable(typeof(JsonStringEnumConverter<SceneObjectKind>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(SceneObjectTransform))]
[JsonSerializable(typeof(Vector2Holder))]
[JsonSerializable(typeof(Vector3Holder))]
[JsonSerializable(typeof(Vector4Holder))]
[JsonSerializable(typeof(List<SceneComponent>))]
internal partial class SceneObjectSerializationContext : JsonSerializerContext
{
}

[MessagePackObject]
public class SerializableSceneHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'S', 'C', 'E'
    };

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializableScene
{
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    public List<SceneObject> objects = new();

    [Key(2)]
    public string typeName = typeof(Scene).FullName;
}

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
[JsonSerializable(typeof(List<SceneObject>))]
[JsonSerializable(typeof(SceneObject))]
internal partial class SerializableSceneSerializationContext : JsonSerializerContext
{
}
