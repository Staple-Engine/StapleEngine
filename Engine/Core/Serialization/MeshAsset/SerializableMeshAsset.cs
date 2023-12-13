using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Internal
{
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
                lhs.typeName == rhs.typeName;
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
                lhs.typeName != rhs.typeName;
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
    public class MeshAssetMeshInfo
    {
        [Key(0)]
        public string name;

        [Key(1)]
        public int materialIndex;

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
    }

    [MessagePackObject]
    public class SerializableMeshAsset
    {
        [Key(0)]
        public MeshAssetMetadata metadata;

        [Key(1)]
        public List<MeshAssetMeshInfo> meshes = new();

        [Key(2)]
        public int materialCount;
    }
}
