using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Staple.Internal;

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
    TextureWrap,
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

    public static bool operator==(Vector2Holder lhs, Vector2Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.x == rhs.x &&
            lhs.y == rhs.y;
    }

    public static bool operator !=(Vector2Holder lhs, Vector2Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.x != rhs.x ||
            lhs.y != rhs.y;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is Vector2Holder rhs)
        {
            return this == rhs;
        }

        return false;
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

    public static bool operator ==(Vector3Holder lhs, Vector3Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.x == rhs.x &&
            lhs.y == rhs.y &&
            lhs.z == rhs.z;
    }

    public static bool operator !=(Vector3Holder lhs, Vector3Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.x != rhs.x ||
            lhs.y != rhs.y ||
            lhs.z != rhs.z;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is Vector3Holder rhs)
        {
            return this == rhs;
        }

        return false;
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

    public Vector4Holder()
    {
    }

    public Vector4Holder(Vector3 v)
    {
        x = v.X;
        y = v.Y;
        z = v.Z;
    }

    public Vector4Holder(Vector4 v)
    {
        x = v.X;
        y = v.Y;
        z = v.Z;
        w = v.W;
    }

    public Vector4 ToVector4()
    {
        return new Vector4(x, y, z, w);
    }

    public static bool operator ==(Vector4Holder lhs, Vector4Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.x == rhs.x &&
            lhs.y == rhs.y &&
            lhs.z == rhs.z &&
            lhs.w == rhs.w;
    }

    public static bool operator !=(Vector4Holder lhs, Vector4Holder rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.x != rhs.x ||
            lhs.y != rhs.y ||
            lhs.z != rhs.z |
            lhs.w == rhs.w;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is Vector4Holder rhs)
        {
            return this == rhs;
        }

        return false;
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

    [Key(7)]
    public TextureWrap textureWrapValue;

    public bool ShouldSerializevec2Value() => vec2Value != null && type == MaterialParameterType.Vector2;

    public bool ShouldSerializevec3Value() => vec3Value != null && type == MaterialParameterType.Vector3;

    public bool ShouldSerializevec4Value() => vec4Value != null && type == MaterialParameterType.Vector4;

    public bool ShouldSerializefloatValue() => type == MaterialParameterType.Float;

    public bool ShouldSerializetextureValue() => type == MaterialParameterType.Texture;

    public bool ShouldSerializecolorValue() => type == MaterialParameterType.Color;

    public bool ShouldSerializetextureWrapValue() => type == MaterialParameterType.TextureWrap;

    public MaterialParameter Clone()
    {
        var outValue = new MaterialParameter()
        {
            type = type,
            textureValue = textureValue,
            colorValue = colorValue,
            floatValue = floatValue,
            textureWrapValue = textureWrapValue,
        };

        if(vec2Value != null)
        {
            outValue.vec2Value = new()
            {
                x = vec2Value.x,
                y = vec2Value.y,
            };
        }

        if(vec3Value != null)
        {
            outValue.vec3Value = new()
            {
                x = vec3Value.x,
                y = vec3Value.y,
                z = vec3Value.z,
            };
        }

        if(vec4Value != null)
        {
            outValue.vec4Value = new()
            {
                x = vec4Value.x,
                y = vec4Value.y,
                z = vec4Value.z,
                w = vec4Value.w,
            };
        }

        return outValue;
    }

    public static bool operator ==(MaterialParameter lhs, MaterialParameter rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.type == rhs.type &&
            lhs.vec2Value == rhs.vec2Value &&
            lhs.vec3Value == rhs.vec3Value &&
            lhs.vec4Value == rhs.vec4Value &&
            lhs.textureValue == rhs.textureValue &&
            lhs.colorValue == rhs.colorValue &&
            lhs.floatValue == rhs.floatValue &&
            lhs.textureWrapValue == rhs.textureWrapValue;
    }

    public static bool operator !=(MaterialParameter lhs, MaterialParameter rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.type != rhs.type ||
            lhs.vec2Value != rhs.vec2Value ||
            lhs.vec3Value != rhs.vec3Value ||
            lhs.vec4Value != rhs.vec4Value ||
            lhs.textureValue != rhs.textureValue ||
            lhs.colorValue != rhs.colorValue ||
            lhs.floatValue != rhs.floatValue ||
            lhs.textureWrapValue != rhs.textureWrapValue;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is MaterialParameter rhs)
        {
            return this == rhs;
        }

        return false;
    }
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
    public string shader;

    [Key(2)]
    public Dictionary<string, MaterialParameter> parameters = new();

    [Key(3)]
    public string typeName = typeof(Material).FullName;

    public bool ShouldSerializeguid() => false;

    public bool ShouldSerializetypeName() => false;

    public static bool operator ==(MaterialMetadata lhs, MaterialMetadata rhs)
    {
        if(lhs is null && rhs is null)
        {
            return true;
        }

        if(lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.guid == rhs.guid &&
            lhs.shader == rhs.shader &&
            lhs.parameters.Count == rhs.parameters.Count &&
            lhs.parameters.Keys.All(x => rhs.parameters.ContainsKey(x) && lhs.parameters[x] == rhs.parameters[x]) &&
            lhs.typeName == rhs.typeName;
    }

    public static bool operator !=(MaterialMetadata lhs, MaterialMetadata rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.guid != rhs.guid ||
            lhs.shader != rhs.shader ||
            lhs.parameters.Count != rhs.parameters.Count ||
            lhs.parameters.Keys.Any(x => rhs.parameters.ContainsKey(x) == false || lhs.parameters[x] != rhs.parameters[x]) ||
            lhs.typeName != rhs.typeName;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is MaterialMetadata rhs)
        {
            return this == rhs;
        }

        return false;
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, MaterialParameter>))]
[JsonSerializable(typeof(MaterialMetadata))]
internal partial class MaterialMetadataSerializationContext : JsonSerializerContext
{
}
