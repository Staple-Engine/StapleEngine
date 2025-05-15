using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ufbx;

#if UFBX_INTERNAL
internal
#else
public
#endif
partial class ufbx
{
    public const uint UFBXNoIndex = ~0u;

    /// <summary>
    /// (Order in which Euler-angle rotation axes are applied for a transform
    /// NOTE: The order in the name refers to the order of axes *applied*,
    /// not the multiplication order: eg. `UFBX_ROTATION_ORDER_XYZ` is `Z*Y*X`
    /// </summary>
    public enum UFBXRotationOrder
    {
        XYZ,
        XZY,
        YZX,
        YXZ,
        ZXY,
        ZYX,
        Spheric
    }

    public enum UFBXDomValueType
    {
        Number,
        String,
        ArrayI8,
        ArrayI16,
        ArrayI64,
        ArrayF32,
        ArrayF64,
        ArrayRawString,
        ArrayIgnored,
    }

    /// <summary>
    /// Data type contained within the property. All the data fields are always
    /// populated regardless of type, so there's no need to switch by type usually
    /// eg. `prop->value_real` and `prop->value_int` have the same value (well, close)
    /// if `prop->type == INTEGER`. String values are not converted from/to.
    /// </summary>
    public enum UFBXPropType
    {
        Unknown,
        Boolean,
        Integer,
        Number,
        Vector,
        Color,
        ColorWithAlpha,
        String,
        DateTime,
        Translation,
        Rotation,
        Scaling,
        Distance,
        Compound,
        Blob,
        Reference,
    }

    /// <summary>
    /// Property flags: Advanced information about properties, not usually needed.
    /// </summary>
    public enum UFBXPropFlags
    {
        /// <summary>
        /// Supports animation.
        /// NOTE: ufbx ignores this and allows animations on non-animatable properties.
        /// </summary>
        Animatable = 0x1,

        /// <summary>
        /// User defined (custom) property.
        /// </summary>
        UserDefined = 0x2,

        /// <summary>
        /// Hidden in UI.
        /// </summary>
        Hidden = 0x4,

        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockX = 0x10,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockY = 0x20,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockZ = 0x40,
        /// <summary>
        /// Disallow modification from UI for components.
        /// </summary>
        LockW = 0x80,

        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteX = 0x100,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteY = 0x200,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteZ = 0x400,
        /// <summary>
        /// Disable animation from components.
        /// </summary>
        MuteW = 0x800,

        /// <summary>
        /// Property created by ufbx when an element has a connected `ufbx_anim_prop`
        /// but doesn't contain the `ufbx_prop` it's referring to.
        /// NOTE: The property may have been found in the templated defaults.
        /// </summary>
        Synthetic = 0x1000,

        /// <summary>
        /// The property has at least one `ufbx_anim_prop` in some layer.
        /// </summary>
        Animated = 0x2000,

        /// <summary>
        /// Used by `ufbx_evaluate_prop()` to indicate the the property was not found.
        /// </summary>
        NBotFound = 0x4000,

        /// <summary>
        /// The property is connected to another one.
        /// This use case is relatively rare so `ufbx_prop` does not track connections
        /// directly. You can find connections from `ufbx_element.connections_dst` where
        /// `ufbx_connection.dst_prop` is this property and `ufbx_connection.src_prop` is defined.
        /// </summary>
        Connected = 0x8000,

        /// <summary>
        /// The value of this property is undefined (represented as zero).
        /// </summary>
        NoValue = 0x10000,

        /// <summary>
        /// This property has been overridden by the user.
        /// See `ufbx_anim.prop_overrides` for more information.
        /// </summary>
        Overriden = 0x20000,

        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Real = 0x100000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec2 = 0x200000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec3 = 0x400000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Vec4 = 0x800000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Int = 0x1000000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        String = 0x2000000,
        /// <summary>
        /// Value type.
        /// `REAL/VEC2/VEC3/VEC4` are mutually exclusive but may coexist with eg. `STRING`
        /// in some rare cases where the string defines the unit for the vector.
        /// </summary>
        Blob = 0x4000000,
    }

    /// <summary>
    /// Null-terminated UTF-8 encoded string within an FBX file
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public unsafe struct UFBXString
    {
        public nint data;
        public ulong length;

        public readonly string AsString
        {
            get
            {
                if(data == nint.Zero)
                {
                    return "";
                }

                return Encoding.UTF8.GetString((byte *)data, (int)length);
            }
        }

        public override readonly string ToString() => AsString;

        public static implicit operator string(UFBXString str) => str.AsString;
    }

    /// <summary>
    /// Opaque byte buffer blob
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXBlob
    {
        public nint data;
        public ulong length;
    }

    /// <summary>
    /// Explicit translation+rotation+scale transformation.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXTransform
    {
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
    }

    /// <summary>
    /// 4x3 matrix encoding an affine transformation.
    /// `cols[0..2]` are the X/Y/Z basis vectors, `cols[3]` is the translation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXMatrix
    {
        public Vector3 column0;
        public Vector3 column1;
        public Vector3 column2;
        public Vector3 column3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVoidList
    {
        public nint data;
        public ulong count;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXBoolList
    {
        public nint data;
        public ulong count;

        public readonly bool[] Value
        {
            get
            {
                if (data == nint.Zero)
                {
                    return [];
                }

                unsafe
                {
                    var array = new bool[count];

                    var span = new Span<byte>((byte*)data, (int)count);

                    for (var i = 0; i < (int)count; i++)
                    {
                        array[i] = span[i] != 0;
                    }

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXUInt32List
    {
        public nint data;
        public ulong count;

        public readonly uint[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new uint[count];

                    var span = new Span<uint>((uint*)data, (int)count);

                    var target = new Span<uint>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXRealList
    {
        public nint data;
        public ulong count;

        public readonly float[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new float[count];

                    var span = new Span<float>((float*)data, (int)count);

                    var target = new Span<float>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVector2List
    {
        public nint data;
        public ulong count;

        public readonly Vector2[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new Vector2[count];

                    var span = new Span<Vector2>((Vector2*)data, (int)count);

                    var target = new Span<Vector2>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVector3List
    {
        public nint data;
        public ulong count;

        public readonly Vector3[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new Vector3[count];

                    var span = new Span<Vector3>((Vector3*)data, (int)count);

                    var target = new Span<Vector3>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXVector4List
    {
        public nint data;
        public ulong count;

        public readonly Vector4[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new Vector4[count];

                    var span = new Span<Vector4>((Vector4*)data, (int)count);

                    var target = new Span<Vector4>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXStringList
    {
        public nint data;
        public ulong count;

        public readonly string[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new string[count];

                    var span = new Span<UFBXString>((UFBXString*)data, (int)count);

                    for(var i = 0; i < (int)count; i++)
                    {
                        array[i] = span[i];
                    }

                    return array;
                }
            }
        }
    }

    [StructLayout (LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomValue
    {
        public UFBXDomValueType type;
        public UFBXString stringValue;
        public UFBXBlob blobValue;
        public long intValue;
        public double floatValue;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomValueList
    {
        public nint data;
        public ulong count;

        public readonly UFBXDomValue[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new UFBXDomValue[count];

                    var span = new Span<UFBXDomValue>((UFBXDomValue*)data, (int)count);

                    var target = new Span<UFBXDomValue>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomNode
    {
        public UFBXString name;
        public UFBXDomNodeList children;
        public UFBXDomValueList values;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXDomNodeList
    {
        public nint data;
        public int count;

        public readonly UFBXDomNode[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new UFBXDomNode[count];

                    var span = new Span<UFBXDomNode>((UFBXDomNode*)data, count);

                    var target = new Span<UFBXDomNode>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProp
    {
        public UFBXString name;

        public uint _internal_key;

        public UFBXPropType type;
        public UFBXPropFlags flags;

        public UFBXString stringValue;
        public UFBXBlob blobValue;
        public long intValue;
        public float realValue0;
        public float realValue1;
        public float realValue2;
        public float realValue3;

        public readonly float realValue => realValue0;

        public readonly Vector2 Vector2Value => new(realValue0, realValue1);

        public readonly Vector3 Vector3Value => new(realValue0, realValue1, realValue2);

        public readonly Vector4 Vector4Value => new(realValue0, realValue1, realValue2, realValue3);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct UFBXPropList
    {
        public nint data;
        public int count;

        public readonly UFBXProp[] Value
        {
            get
            {
                if (data == nint.Zero || count < 0)
                {
                    return [];
                }

                unsafe
                {
                    var array = new UFBXProp[count];

                    var span = new Span<UFBXProp>((UFBXProp*)data, count);

                    var target = new Span<UFBXProp>(array);

                    span.CopyTo(target);

                    return array;
                }
            }
        }
    }

    [StructLayout (LayoutKind.Sequential, Pack = 0)]
    public struct UFBXProps
    {
        public UFBXPropList props;
        public ulong numAnimated;

        public nint defaults;

        public readonly bool TryGetDefaults(out UFBXProps props)
        {
            if(defaults == nint.Zero)
            {
                props = default;

                return false;
            }

            unsafe
            {
                props = *(UFBXProps*)defaults;
            }

            return true;
        }
    }
}
