using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

public class MeshAsset
{
    public class MeshInfo
    {
        public string name;
        public int materialIndex;
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
        public AABB bounds;
    }

    public List<MeshInfo> meshes = new();

    public int materialCount;
}
