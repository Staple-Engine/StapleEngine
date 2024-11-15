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
    /// The UI image texture. Should be a sprite type texture.
    /// </summary>
    public Texture texture;

    /// <summary>
    /// The sprite index in the texture
    /// </summary>
    public int spriteIndex = 0;

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

    private static SpriteRenderSystem.SpriteVertex[] vertices = new SpriteRenderSystem.SpriteVertex[4];

    private static readonly ushort[] indices = [0, 1, 2, 2, 3, 0];

    private void SetMaterial()
    {
        material.shader.SetColor(material.GetShaderHandle(Material.MainColorProperty), color);
        material.shader.SetTexture(material.GetShaderHandle(Material.MainTextureProperty), texture);

        material.DisableShaderKeyword(Shader.SkinningKeyword);
    }

    public override void Render(Vector2Int position, ushort viewID)
    {
        if (texture == null ||
            texture.Disposed ||
            material == null ||
            material.Disposed ||
            material.shader == null ||
            material.shader.Disposed ||
            spriteIndex < 0 ||
            spriteIndex >= texture.metadata.sprites.Count)
        {
            return;
        }

        var sprite = texture.metadata.sprites[spriteIndex];

        vertices[0].position = new(0, size.Y, 0);
        vertices[0].uv = new(sprite.rect.left / (float)texture.Width, sprite.rect.bottom / (float)texture.Height);
        vertices[1].position = Vector3.Zero;
        vertices[1].uv = new(sprite.rect.left / (float)texture.Width, sprite.rect.top / (float)texture.Height);
        vertices[2].position = new(size.X, 0, 0);
        vertices[2].uv = new(sprite.rect.right / (float)texture.Width, sprite.rect.top / (float)texture.Height);
        vertices[3].position = new(size.X, size.Y, 0);
        vertices[3].uv = new(sprite.rect.right / (float)texture.Width, sprite.rect.bottom / (float)texture.Height);

        var vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(indices.AsSpan());

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertices.Length, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            viewID, SetMaterial);
    }
}
