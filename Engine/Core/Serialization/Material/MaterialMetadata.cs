using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal
{
    public enum MaterialParameterType
    {
        Vector2,
        Vector3,
        Vector4,
        Texture,
        Color,
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
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialMetadata
    {
        [Key(0)]
        public string shaderPath;

        [Key(1)]
        public Dictionary<string, MaterialParameter> parameters = new();
    }
}
