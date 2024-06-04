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

        var vertices = new SpriteRenderSystem.SpriteVertex[]
        {
            new()
            {
                position = new Vector3(0, size.Y, 0),
                uv = new(sprite.rect.left / (float)texture.Width, sprite.rect.bottom / (float)texture.Height),
            },
            new()
            {
                position = Vector3.Zero,
                uv = new(sprite.rect.left / (float)texture.Width, sprite.rect.top / (float)texture.Height),
            },
            new()
            {
                position = new Vector3(size.X, 0, 0),
                uv = new(sprite.rect.right / (float)texture.Width, sprite.rect.top / (float)texture.Height),
            },
            new()
            {
                position = new Vector3(size.X, size.Y, 0),
                uv = new(sprite.rect.right / (float)texture.Width, sprite.rect.bottom / (float)texture.Height),
            },
        };

        var vertexBuffer = VertexBuffer.Create(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value, true);

        var indexBuffer = IndexBuffer.Create((ushort[])[0, 1, 2, 2, 3, 0], RenderBufferFlags.None, true);

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertices.Length, 0, 6, material,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, viewID, () =>
            {
                material.shader.SetColor(Material.MainColorProperty, color);
                material.shader.SetTexture(Material.MainTextureProperty, texture);

                material.DisableShaderKeyword(Shader.SkinningKeyword);
            });
    }
}
