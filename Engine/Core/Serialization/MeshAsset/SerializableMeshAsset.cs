using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

public enum MeshAssetType
{
    Normal,
    Skinned,
}

public enum MeshAssetRotation
{
    None,
    NinetyPositive,
    NinetyNegative,
}

[MessagePackObject]
public class SerializableMeshAssetHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader =
    [
        'S', 'M', 'E', 'A'
    ];

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[Serializable]
[MessagePackObject]
public class MeshAssetMetadata
{
    [HideInInspector]
    [Key(0)]
    public string guid;

    [Key(1)]
    public bool flipUVs = false;

    [Key(2)]
    public bool flipWindingOrder = true;

    [Key(3)]
    public bool regenerateNormals;

    [Key(4)]
    public bool useSmoothNormals;

    [Key(5)]
    public MaterialLighting lighting = MaterialLighting.Lit;

    [Key(6)]
    public MeshAssetRotation rotation;

    [Key(7)]
    public float scale = 1.0f;

    [Key(8)]
    public int frameRate = 60;

    [Key(9)]
    [Tooltip("Whether to sync the animation to the screen refresh rate")]
    public bool syncAnimationToRefreshRate = false;

    [HideInInspector]
    [Key(10)]
    public string typeName = typeof(Mesh).FullName;

    public static bool operator==(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        if(lhs is null)
        {
            return rhs is null;
        }

        if(rhs is null)
        {
            return lhs is null;
        }

        return lhs.guid == rhs.guid &&
            lhs.flipUVs == rhs.flipUVs &&
            lhs.flipWindingOrder == rhs.flipWindingOrder &&
            lhs.typeName == rhs.typeName &&
            lhs.regenerateNormals == rhs.regenerateNormals &&
            lhs.useSmoothNormals == rhs.useSmoothNormals &&
            lhs.lighting == rhs.lighting &&
            lhs.rotation == rhs.rotation &&
            lhs.scale == rhs.scale &&
            lhs.frameRate == rhs.frameRate &&
            lhs.syncAnimationToRefreshRate == rhs.syncAnimationToRefreshRate;
    }

    public static bool operator!=(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        if (lhs is null)
        {
            return rhs is not null;
        }

        if (rhs is null)
        {
            return lhs is not null;
        }

        return lhs.guid != rhs.guid ||
            lhs.flipUVs != rhs.flipUVs ||
            lhs.flipWindingOrder != rhs.flipWindingOrder ||
            lhs.typeName != rhs.typeName ||
            lhs.regenerateNormals != rhs.regenerateNormals ||
            lhs.useSmoothNormals != rhs.useSmoothNormals ||
            lhs.lighting != rhs.lighting ||
            lhs.rotation != rhs.rotation ||
            lhs.scale != rhs.scale ||
            lhs.frameRate != rhs.frameRate ||
            lhs.syncAnimationToRefreshRate != rhs.syncAnimationToRefreshRate;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is MeshAssetMetadata rhs)
        {
            return this == rhs;
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(guid);
        hash.Add(flipUVs);
        hash.Add(flipWindingOrder);
        hash.Add(regenerateNormals);
        hash.Add(useSmoothNormals);
        hash.Add(lighting);
        hash.Add(rotation);
        hash.Add(scale);
        hash.Add(frameRate);
        hash.Add(typeName);

        return hash.ToHashCode();
    }
}

[MessagePackObject]
public class Matrix4x4Holder
{
    [Key(0)]
    public float M11;
    [Key(1)]
    public float M12;
    [Key(2)]
    public float M13;
    [Key(3)]
    public float M14;
    [Key(4)]
    public float M21;
    [Key(5)]
    public float M22;
    [Key(6)]
    public float M23;
    [Key(7)]
    public float M24;
    [Key(8)]
    public float M31;
    [Key(9)]
    public float M32;
    [Key(10)]
    public float M33;
    [Key(11)]
    public float M34;
    [Key(12)]
    public float M41;
    [Key(13)]
    public float M42;
    [Key(14)]
    public float M43;
    [Key(15)]
    public float M44;

    public Matrix4x4 ToMatrix()
    {
        return new(M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44);
    }

    public static Matrix4x4Holder FromMatrix(Matrix4x4 matrix)
    {
        return new()
        {
            M11 = matrix.M11,
            M12 = matrix.M12,
            M13 = matrix.M13,
            M14 = matrix.M14,
            M21 = matrix.M21,
            M22 = matrix.M22,
            M23 = matrix.M23,
            M24 = matrix.M24,
            M31 = matrix.M31,
            M32 = matrix.M32,
            M33 = matrix.M33,
            M34 = matrix.M34,
            M41 = matrix.M41,
            M42 = matrix.M42,
            M43 = matrix.M43,
            M44 = matrix.M44,
        };
    }
}

[MessagePackObject]
public class MeshAssetBone
{
    [Key(0)]
    public int nodeIndex;

    [Key(1)]
    public Matrix4x4Holder offsetMatrix;
}

[MessagePackObject]
public class MeshAssetMeshInfo
{
    [Key(0)]
    public string name;

    [Key(1)]
    public string materialGuid;

    [Key(2)]
    public MeshTopology topology;

    [Key(3)]
    public List<Vector3Holder> vertices = [];

    [Key(4)]
    public List<Vector4Holder> colors = [];

    [Key(5)]
    public List<Vector3Holder> normals = [];

    [Key(6)]
    public List<Vector3Holder> tangents = [];

    [Key(7)]
    public List<Vector3Holder> bitangents = [];

    [Key(8)]
    public List<int> indices = [];

    [Key(9)]
    public Vector3Holder boundsCenter;

    [Key(10)]
    public Vector3Holder boundsExtents;

    [Key(11)]
    public List<Vector2Holder> UV1 = [];

    [Key(12)]
    public List<Vector2Holder> UV2 = [];

    [Key(13)]
    public List<Vector2Holder> UV3 = [];

    [Key(14)]
    public List<Vector2Holder> UV4 = [];

    [Key(15)]
    public List<Vector2Holder> UV5 = [];

    [Key(16)]
    public List<Vector2Holder> UV6 = [];

    [Key(17)]
    public List<Vector2Holder> UV7 = [];

    [Key(18)]
    public List<Vector2Holder> UV8 = [];

    [Key(19)]
    public MeshAssetType type;

    [Key(20)]
    public List<MeshAssetBone> bones = [];

    [Key(21)]
    public List<Vector4Holder> boneIndices = [];

    [Key(22)]
    public List<Vector4Holder> boneWeights = [];

    [Key(23)]
    public List<Vector4Holder> colors2 = [];

    [Key(24)]
    public List<Vector4Holder> colors3 = [];

    [Key(25)]
    public List<Vector4Holder> colors4 = [];
}

[MessagePackObject]
public class MeshAssetNode
{
    [Key(0)]
    public string name;

    [Key(1)]
    public Vector3Holder position;

    [Key(2)]
    public Vector3Holder rotation;

    [Key(3)]
    public Vector3Holder scale;

    [Key(4)]
    public List<int> children = [];

    [Key(5)]
    public List<int> meshIndices = [];
}

[MessagePackObject]
public class MeshAssetVectorAnimationKey
{
    [Key(0)]
    public float time;

    [Key(1)]
    public Vector3Holder value;
}

[MessagePackObject]
public class MeshAssetQuaternionAnimationKey
{
    [Key(0)]
    public float time;

    [Key(1)]
    public Vector4Holder value;
}

[MessagePackObject]
public class MeshAssetAnimationChannel
{
    [Key(0)]
    public int nodeIndex;

    [Key(1)]
    public List<MeshAssetVectorAnimationKey> positionKeys = [];

    [Key(2)]
    public List<MeshAssetQuaternionAnimationKey> rotationKeys = [];

    [Key(3)]
    public List<MeshAssetVectorAnimationKey> scaleKeys = [];
}

[MessagePackObject]
public class MeshAssetAnimation
{
    [Key(0)]
    public string name;

    [Key(1)]
    public float duration;

    [Key(2)]
    public List<MeshAssetAnimationChannel> channels = [];
}

[MessagePackObject]
public class SerializableMeshAsset
{
    [Key(0)]
    public MeshAssetMetadata metadata;

    [Key(1)]
    public List<MeshAssetMeshInfo> meshes = [];

    [Key(2)]
    public MeshAssetNode[] nodes;

    [Key(3)]
    public List<MeshAssetAnimation> animations = [];
}
