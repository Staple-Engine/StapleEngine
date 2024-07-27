using Staple.Internal;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

/// <summary>
/// Helper class to show gizmos in the scene view
/// </summary>
public static class Gizmo
{
    private static Material meshMaterial;
    private static Mesh wireCube = null;
    private static VertexLayout lineLayout = null;

    /// <summary>
    /// Shows a box
    /// </summary>
    /// <param name="position">The position of the box</param>
    /// <param name="rotation">The rotation of the box</param>
    /// <param name="scale">The scale of the box</param>
    /// <param name="color">The color of the box</param>
    public static void Box(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        MeshRenderSystem.DrawMesh(Mesh.Cube, position, rotation, scale, meshMaterial, StapleEditor.WireframeView);
    }

    /// <summary>
    /// Shows a wireframe box
    /// </summary>
    /// <param name="position">The position of the box</param>
    /// <param name="rotation">The rotation of the box</param>
    /// <param name="scale">The scale of the box</param>
    /// <param name="color">The color of the box</param>
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

            wireCube.vertices =
            [
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
            ];

            wireCube.indices = wireCube.vertices.Select((x, xIndex) => xIndex).ToArray();

            wireCube.UploadMeshData();
        }

        MeshRenderSystem.DrawMesh(wireCube, position, rotation, scale, meshMaterial, StapleEditor.WireframeView);
    }

    public static void Line(Vector3 from, Vector3 to, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        lineLayout ??= new VertexLayoutBuilder()
            .Add(Bgfx.bgfx.Attrib.Position, 3, Bgfx.bgfx.AttribType.Float)
            .Build();

        Graphics.RenderSimple([from, to], lineLayout, [0, 1], meshMaterial, Matrix4x4.Identity, MeshTopology.Lines, StapleEditor.WireframeView);
    }
}
