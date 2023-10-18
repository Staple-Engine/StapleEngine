using Staple.Internal;
using System.Numerics;

namespace Staple.Editor
{
    public static class Gizmo
    {
        private static Material meshMaterial;

        public static void Box(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

            meshMaterial.MainColor = color;

            MeshRenderSystem.DrawMesh(Mesh.Cube, position, rotation, scale, meshMaterial, StapleEditor.WireframeView);
        }
    }
}
