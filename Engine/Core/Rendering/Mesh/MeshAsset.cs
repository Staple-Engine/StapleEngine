using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    public class MeshAsset : IPathAsset
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
            public AABB bounds;
        }

        public List<MeshInfo> meshes = new();

        public int materialCount;

        public string Path { get; set; }

        public static object Create(string path)
        {
            return ResourceManager.instance.LoadMeshAsset(path);
        }
    }
}
