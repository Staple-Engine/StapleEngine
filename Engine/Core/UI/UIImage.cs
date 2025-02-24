using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.UI;

/// <summary>
/// UI Image component, used to display sprites in the UI. This component has no intrinsic size.
/// </summary>
public class UIImage : UIElement
{
    /// <summary>
    /// The UI image sprite.
    /// </summary>
    public Sprite sprite;

    /// <summary>
    /// The color to use for displaying the sprite
    /// </summary>
    public Color color = Color.White;

    /// <summary>
    /// The material to use
    /// </summary>
    public Material material;

    public UIImage()
    {
        adjustToIntrinsicSize = false;
    }

    public override Vector2Int IntrinsicSize()
    {
        return Vector2Int.Zero;
    }

    private static readonly SpriteRenderSystem.SpriteVertex[] vertices = new SpriteRenderSystem.SpriteVertex[4];

    private static readonly ushort[] indices = [0, 1, 2, 2, 3, 0];

    private void SetMaterial()
    {
        material.shader.SetColor(material.GetShaderHandle(Material.MainColorProperty), color);
        material.shader.SetTexture(material.GetShaderHandle(Material.MainTextureProperty), sprite.texture);

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);
    }

    public override void Render(Vector2Int position, ushort viewID)
    {
        if (sprite == null ||
            sprite.IsValid == false ||
            material == null ||
            material.Disposed ||
            material.shader == null ||
            material.shader.Disposed)
        {
            return;
        }

        var rect = sprite.Rect;

        vertices[0].position = new(0, size.Y, 0);
        vertices[0].uv = new(rect.left / (float)sprite.texture.Width, rect.bottom / (float)sprite.texture.Height);
        vertices[1].position = Vector3.Zero;
        vertices[1].uv = new(rect.left / (float)sprite.texture.Width, rect.top / (float)sprite.texture.Height);
        vertices[2].position = new(size.X, 0, 0);
        vertices[2].uv = new(rect.right / (float)sprite.texture.Width, rect.top / (float)sprite.texture.Height);
        vertices[3].position = new(size.X, size.Y, 0);
        vertices[3].uv = new(rect.right / (float)sprite.texture.Width, rect.bottom / (float)sprite.texture.Height);

        var vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(indices);

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertices.Length, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            viewID, SetMaterial);
    }
}
