﻿using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

public enum MeshAssetAnimationStateBehaviour
{
    Default,
    Constant,
    Linear,
    Repeat,
}

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
    [HideInInspector]
    [Key(0)]
    public string guid;

    [Key(1)]
    public bool makeLeftHanded;

    [Key(2)]
    public bool splitLargeMeshes;

    [Key(3)]
    public bool preTransformVertices;

    [Key(4)]
    public bool flipUVs = true;

    [Key(5)]
    public bool flipWindingOrder = true;

    [Key(6)]
    public bool splitByBoneCount;

    [Key(7)]
    public bool debone;

    [Key(8)]
    public bool convertUnits = true;

    [Key(9)]
    public bool regenerateNormals;

    [Key(10)]
    public bool useSmoothNormals;

    [Key(11)]
    public MeshAssetRotation rotation = MeshAssetRotation.None;

    [Key(12)]
    public float scale = 1.0f;

    [Key(13)]
    public MaterialLighting lighting = MaterialLighting.Lit;

    [Key(14)]
    public int frameRate = 30;

    [HideInInspector]
    [Key(15)]
    public string typeName = typeof(Mesh).FullName;

    public static bool operator ==(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        return lhs.guid == rhs.guid &&
            lhs.makeLeftHanded == rhs.makeLeftHanded &&
            lhs.splitLargeMeshes == rhs.splitLargeMeshes &&
            lhs.preTransformVertices == rhs.preTransformVertices &&
            lhs.flipUVs == rhs.flipUVs &&
            lhs.flipWindingOrder == rhs.flipWindingOrder &&
            lhs.splitByBoneCount == rhs.splitByBoneCount &&
            lhs.debone == rhs.debone &&
            lhs.typeName == rhs.typeName &&
            lhs.convertUnits == rhs.convertUnits &&
            lhs.regenerateNormals == rhs.regenerateNormals &&
            lhs.useSmoothNormals == rhs.useSmoothNormals &&
            lhs.rotation == rhs.rotation &&
            lhs.scale == rhs.scale &&
            lhs.lighting == rhs.lighting &&
            lhs.frameRate == rhs.frameRate;
    }

    public static bool operator !=(MeshAssetMetadata lhs, MeshAssetMetadata rhs)
    {
        return lhs.guid != rhs.guid ||
            lhs.makeLeftHanded != rhs.makeLeftHanded ||
            lhs.splitLargeMeshes != rhs.splitLargeMeshes ||
            lhs.preTransformVertices != rhs.preTransformVertices ||
            lhs.flipUVs != rhs.flipUVs ||
            lhs.flipWindingOrder != rhs.flipWindingOrder ||
            lhs.splitByBoneCount != rhs.splitByBoneCount ||
            lhs.debone != rhs.debone ||
            lhs.typeName != rhs.typeName ||
            lhs.convertUnits != rhs.convertUnits ||
            lhs.regenerateNormals != rhs.regenerateNormals ||
            lhs.useSmoothNormals != rhs.useSmoothNormals ||
            lhs.rotation != rhs.rotation ||
            lhs.scale != rhs.scale ||
            lhs.lighting != rhs.lighting ||
            lhs.frameRate != rhs.frameRate;
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
public class MeshAssetBone
{
    [Key(0)]
    public string name;

    [Key(1)]
    public Vector3Holder offsetPosition;

    [Key(2)]
    public Vector3Holder offsetRotation;

    [Key(3)]
    public Vector3Holder offsetScale;
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

    [Key(21)]
    public List<Vector4Holder> boneIndices = new();

    [Key(22)]
    public List<Vector4Holder> boneWeights = new();
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
    public List<MeshAssetNode> children = new();

    [Key(5)]
    public List<int> meshIndices = new();
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
    public string nodeName;

    [Key(1)]
    public List<MeshAssetVectorAnimationKey> positionKeys = new();

    [Key(2)]
    public List<MeshAssetQuaternionAnimationKey> rotationKeys = new();

    [Key(3)]
    public List<MeshAssetVectorAnimationKey> scaleKeys = new();

    [Key(4)]
    public MeshAssetAnimationStateBehaviour preState;

    [Key(5)]
    public MeshAssetAnimationStateBehaviour postState;
}

[MessagePackObject]
public class MeshAssetAnimation
{
    [Key(0)]
    public string name;

    [Key(1)]
    public float duration;

    [Key(2)]
    public float ticksPerSecond;

    [Key(3)]
    public List<MeshAssetAnimationChannel> channels = new();
}

[MessagePackObject]
public class SerializableMeshAsset
{
    [Key(0)]
    public MeshAssetMetadata metadata;

    [Key(1)]
    public List<MeshAssetMeshInfo> meshes = new();

    [Key(2)]
    public MeshAssetNode rootNode;

    [Key(3)]
    public List<MeshAssetAnimation> animations = new();
}
