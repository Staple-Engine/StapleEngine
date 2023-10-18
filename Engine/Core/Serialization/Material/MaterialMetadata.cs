using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Staple.Internal
{
    [JsonConverter(typeof(JsonStringEnumConverter<MaterialParameterType>))]
    public enum MaterialParameterType
    {
        Vector2,
        Vector3,
        Vector4,
        Texture,
        Color,
        Float,
        Matrix3x3,
        Matrix4x4,
    }

    [Serializable]
    [MessagePackObject]
    public class Vector2Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, 0);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(x, y, 0, 0);
        }
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(Vector2Holder))]
    internal partial class Vector2HolderSerializationContext : JsonSerializerContext
    {
    }

    [Serializable]
    [MessagePackObject]
    public class Vector3Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        [Key(2)]
        public float z;

        public Vector3Holder()
        {
        }

        public Vector3Holder(Vector3 v)
        {
            x = v.X;
            y = v.Y;
            z = v.Z;
        }

        public Vector3Holder(Quaternion q) : this(Math.ToEulerAngles(q))
        {
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(x, y, z, 0);
        }

        public Quaternion ToQuaternion()
        {
            return Math.FromEulerAngles(ToVector3());
        }
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(Vector3Holder))]
    internal partial class Vector3HolderSerializationContext : JsonSerializerContext
    {
    }

    [Serializable]
    [MessagePackObject]
    public class Vector4Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        [Key(2)]
        public float z;

        [Key(3)]
        public float w;

        public Vector4 ToVector4()
        {
            return new Vector4(x, y, z, w);
        }
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(Vector4Holder))]
    internal partial class Vector4HolderSerializationContext : JsonSerializerContext
    {
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialParameter
    {
        [Key(0)]
        public MaterialParameterType type;

        [Key(1)]
        public Vector2Holder vec2Value;

        [Key(2)]
        public Vector3Holder vec3Value;

        [Key(3)]
        public Vector4Holder vec4Value;

        [Key(4)]
        public string textureValue;

        [Key(5)]
        public Color32 colorValue;

        [Key(6)]
        public float floatValue;
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(JsonStringEnumConverter<MaterialParameterType>))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Color32))]
    [JsonSerializable(typeof(Vector2Holder))]
    [JsonSerializable(typeof(Vector3Holder))]
    [JsonSerializable(typeof(Vector4Holder))]
    [JsonSerializable(typeof(MaterialParameter))]
    internal partial class MaterialParameterSerializationContext : JsonSerializerContext
    {
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialMetadata
    {
        [Key(0)]
        public string guid = Guid.NewGuid().ToString();

        [Key(1)]
        public string shaderPath;

        [Key(2)]
        public Dictionary<string, MaterialParameter> parameters = new();

        [Key(3)]
        public string typeName = typeof(Material).FullName;
    }

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Dictionary<string, MaterialParameter>))]
    [JsonSerializable(typeof(MaterialMetadata))]
    internal partial class MaterialMetadataSerializationContext : JsonSerializerContext
    {
    }
}
