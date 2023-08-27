using MessagePack;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.Internal
{
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

    [MessagePackObject]
    public class SceneComponentParameter
    {
        [Key(0)]
        public string name;

        [Key(1)]
        public SceneComponentParameterType type;

        [Key(2)]
        public bool boolValue;

        [Key(3)]
        public int intValue;

        [Key(4)]
        public float floatValue;

        [Key(5)]
        public string stringValue;

        [Key(6)]
        public Vector2Holder vector2Value;

        [Key(7)]
        public Vector3Holder vector3Value;

        [Key(8)]
        public Vector4Holder vector4Value;
    }

    [Serializable]
    [MessagePackObject]
    public class SceneComponent
    {
        [Key(0)]
        public string type;

        [Key(1)]
        [JsonIgnore]
        public List<SceneComponentParameter> parameters = new();

        [IgnoreMember]
        public Dictionary<string, object> data;
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(int))]
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
        public List<SceneComponent> components = new();

        [Key(6)]
        public string layer;
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(SceneObject))]
    [JsonSerializable(typeof(List<SceneObject>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<SceneObjectKind>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(int))]
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
        public List<SceneObject> objects = new();
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(List<SceneObject>))]
    [JsonSerializable(typeof(SceneObject))]
    internal partial class SerializableSceneSerializationContext : JsonSerializerContext
    {
    }
}
