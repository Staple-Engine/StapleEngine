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
public class Matrix4x4Holder
{
    [Key(1)]
    public float[] values = new float[16];

    public Matrix4x4Holder()
    {
    }

    public Matrix4x4Holder(Matrix4x4 matrix)
    {
        values =
        [
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44,
        ];
    }

    public bool ToMatrix4x4(out Matrix4x4 matrix)
    {
        if(values == null || values.Length != 16)
        {
            matrix = default;

            return false;
        }

        matrix = new Matrix4x4(values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]);

        return true;
    }
}

[MessagePackObject]
public class SerializableMeshAssetHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'M', 'E', 'A'
    };

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
    [Key(0)]
    public string guid;

    [Key(1)]
    public bool makeLeftHanded;

    [Key(2)]
    public bool splitLargeMeshes;

    [Key(3)]
    public bool preTransformVertices;

    [Key(4)]
    public bool limitBoneWeights;

    [Key(5)]
    public bool flipUVs;

    [Key(6)]
    public bool flipWindingOrder;

    [Key(7)]
    public bool splitByBoneCount;

    [Key(8)]
    public bool debone;

    [Key(9)]
    public string typeName = typeof(Mesh).FullName;

    [Key(10)]
    public bool convertUnits = true;

    [Key(11)]
    public MeshAssetRotation rotation = MeshAssetRotation.None;

    [Key(12)]
    public float scale = 1.0f;

    public static bool operator ==(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        return lhs.guid == rhs.guid &&
            lhs.makeLeftHanded == rhs.makeLeftHanded &&
            lhs.splitLargeMeshes == rhs.splitLargeMeshes &&
            lhs.preTransformVertices == rhs.preTransformVertices &&
            lhs.limitBoneWeights == rhs.limitBoneWeights &&
            lhs.flipUVs == rhs.flipUVs &&
            lhs.flipWindingOrder == rhs.flipWindingOrder &&
            lhs.splitByBoneCount == rhs.splitByBoneCount &&
            lhs.debone == rhs.debone &&
            lhs.typeName == rhs.typeName &&
            lhs.convertUnits == rhs.convertUnits &&
            lhs.rotation == rhs.rotation &&
            lhs.scale == rhs.scale;
    }

    public static bool operator !=(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        return lhs.guid != rhs.guid ||
            lhs.makeLeftHanded != rhs.makeLeftHanded ||
            lhs.splitLargeMeshes != rhs.splitLargeMeshes ||
            lhs.preTransformVertices != rhs.preTransformVertices ||
            lhs.limitBoneWeights != rhs.limitBoneWeights ||
            lhs.flipUVs != rhs.flipUVs ||
            lhs.flipWindingOrder != rhs.flipWindingOrder ||
            lhs.splitByBoneCount != rhs.splitByBoneCount ||
            lhs.debone != rhs.debone ||
            lhs.typeName != rhs.typeName ||
            lhs.convertUnits != rhs.convertUnits ||
            lhs.rotation != rhs.rotation ||
            lhs.scale != rhs.scale;
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
}

[MessagePackObject]
public class MeshAssetVertexWeight
{
    [Key(0)]
    public int vertexID;

    [Key(1)]
    public float weight;
}

[MessagePackObject]
public class MeshAssetBone
{
    [Key(0)]
    public string name;

    [Key(1)]
    public Matrix4x4Holder offsetMatrix;

    [Key(2)]
    public List<MeshAssetVertexWeight> weights = new();
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
    public List<Vector3Holder> vertices = new();

    [Key(4)]
    public List<Vector4Holder> colors = new();

    [Key(5)]
    public List<Vector3Holder> normals = new();

    [Key(6)]
    public List<Vector3Holder> tangents = new();

    [Key(7)]
    public List<Vector3Holder> bitangents = new();

    [Key(8)]
    public List<int> indices = new();

    [Key(9)]
    public Vector3Holder boundsCenter;

    [Key(10)]
    public Vector3Holder boundsExtents;

    [Key(11)]
    public List<Vector2Holder> UV1 = new();

    [Key(12)]
    public List<Vector2Holder> UV2 = new();

    [Key(13)]
    public List<Vector2Holder> UV3 = new();

    [Key(14)]
    public List<Vector2Holder> UV4 = new();

    [Key(15)]
    public List<Vector2Holder> UV5 = new();

    [Key(16)]
    public List<Vector2Holder> UV6 = new();

    [Key(17)]
    public List<Vector2Holder> UV7 = new();

    [Key(18)]
    public List<Vector2Holder> UV8 = new();

    [Key(19)]
    public MeshAssetType type;

    [Key(20)]
    public List<MeshAssetBone> bones = new();
}

[MessagePackObject]
public class SerializableMeshAsset
{
    [Key(0)]
    public MeshAssetMetadata metadata;

    [Key(1)]
    public List<MeshAssetMeshInfo> meshes = new();
}
