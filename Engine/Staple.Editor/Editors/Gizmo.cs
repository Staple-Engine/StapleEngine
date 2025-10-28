using Staple.Internal;
using System;
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
    private static Mesh wireSphere = null;
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

        MeshRenderSystem.RenderMesh(Mesh.Cube, position, rotation, scale, meshMaterial, MaterialLighting.Unlit, StapleEditor.WireframeView);
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
            wireCube = new Mesh(true, false)
            {
                meshTopology = MeshTopology.LineStrip,
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
        }

        MeshRenderSystem.RenderMesh(wireCube, position, rotation, scale, meshMaterial, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }
    /// <summary>
    /// Shows a sphere
    /// </summary>
    /// <param name="position">The position of the sphere</param>
    /// <param name="rotation">The rotation of the sphere</param>
    /// <param name="scale">The scale of the sphere</param>
    /// <param name="color">The color of the sphere</param>
    public static void Sphere(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        MeshRenderSystem.RenderMesh(Mesh.Sphere, position, rotation, scale, meshMaterial, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }

    /// <summary>
    /// Shows a wireframe sphere
    /// </summary>
    /// <param name="position">The position of the sphere</param>
    /// <param name="rotation">The rotation of the sphere</param>
    /// <param name="scale">The scale of the sphere</param>
    /// <param name="color">The color of the sphere</param>
    public static void WireframeSphere(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        if (wireSphere == null)
        {
            wireSphere = Mesh.GenerateSphere(18, 9, 0.5f, true);

            wireSphere.MeshTopology = MeshTopology.LineStrip;
        }

        MeshRenderSystem.RenderMesh(wireSphere, position, rotation, scale, meshMaterial, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }

    public static void Line(Vector3 from, Vector3 to, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        lineLayout ??= VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Build();

        Graphics.RenderSimple([from, to], lineLayout, [0, 1], meshMaterial, Vector3.Zero, Matrix4x4.Identity,
            MeshTopology.Lines, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }

    public static void Lines(Span<Vector3> points, Span<ushort> indices, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        lineLayout ??= VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Build();

        Graphics.RenderSimple(points, lineLayout, indices, meshMaterial, Vector3.Zero, Matrix4x4.Identity,
            MeshTopology.Lines, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }

    public static void Lines(Span<Vector3> points, Span<uint> indices, Color color)
    {
        meshMaterial ??= new Material(StapleEditor.instance.wireframeMaterial);

        meshMaterial.MainColor = color;

        lineLayout ??= VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Build();

        Graphics.RenderSimple(points, lineLayout, indices, meshMaterial, Vector3.Zero, Matrix4x4.Identity,
            MeshTopology.Lines, MaterialLighting.Unlit, StapleEditor.WireframeView);
    }
}
