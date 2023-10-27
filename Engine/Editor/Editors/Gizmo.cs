using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    public static class Gizmo
    {
        private static Material meshMaterial;
        private static Mesh wireCube = null;

        public static void Box(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

            meshMaterial.MainColor = color;

            MeshRenderSystem.DrawMesh(Mesh.Cube, position, rotation, scale, meshMaterial, StapleEditor.WireframeView);
        }

        public static void WireframeBox(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

            meshMaterial.MainColor = color;

            if(wireCube == null)
            {
                wireCube = new Mesh
                {
                    MeshTopology = MeshTopology.LineStrip,
                };

                wireCube.vertices = new Vector3[]
                {
                    //back
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f),

                    //down
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f),

                    //left
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f),

                    //up
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),

                    //front
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),

                    //No need for right
                };

                wireCube.indices = wireCube.vertices.Select((x, xIndex) => xIndex).ToArray();

                wireCube.UploadMeshData();
            }

            MeshRenderSystem.DrawMesh(wireCube, position, rotation, scale, meshMaterial, StapleEditor.WireframeView);
        }
    }
}
