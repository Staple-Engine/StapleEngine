using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public class MeshAsset
{
    public class MeshInfo
    {
        public string name;
        public string materialGuid;
        public MeshAssetType type;
        public MeshTopology topology;
        public List<Vector3> vertices = new();
        public List<Vector3> normals = new();
        public List<Color> colors = new();
        public List<Vector3> tangents = new();
        public List<Vector3> bitangents = new();
        public List<Vector2> UV1 = new();
        public List<Vector2> UV2 = new();
        public List<Vector2> UV3 = new();
        public List<Vector2> UV4 = new();
        public List<Vector2> UV5 = new();
        public List<Vector2> UV6 = new();
        public List<Vector2> UV7 = new();
        public List<Vector2> UV8 = new();
        public List<int> indices = new();
        public List<Vector4> boneIndices = new();
        public List<Vector4> boneWeights = new();
        public AABB bounds;
    }

    public List<MeshInfo> meshes = new();
}
