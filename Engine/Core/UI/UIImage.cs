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

    /// <summary>
    /// How to render the sprite
    /// </summary>
    public SpriteRenderMode renderMode = SpriteRenderMode.Normal;

    private SpriteRenderSystem.SpriteVertex[] ninePatchVertices = [];
    private uint[] ninePatchIndices = [];

    private Sprite lastSprite;

    private Vector2Int lastSize;

    private static readonly SpriteRenderSystem.SpriteVertex[] vertices = new SpriteRenderSystem.SpriteVertex[4];

    private static readonly ushort[] indices = [0, 1, 2, 2, 3, 0];

    public UIImage()
    {
        adjustToIntrinsicSize = false;
    }

    public override Vector2Int IntrinsicSize()
    {
        return Vector2Int.Zero;
    }

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

        VertexBuffer vertexBuffer = null;
        IndexBuffer indexBuffer = null;

        var vertexCount = vertices.Length;
        var indexCount = indices.Length;

        switch(renderMode)
        {
            case SpriteRenderMode.Normal:
                {
                    var rect = sprite.Rect;

                    vertices[0].position = new(0, size.Y, 0);
                    vertices[0].uv = new(rect.left / (float)sprite.texture.Width, rect.bottom / (float)sprite.texture.Height);
                    vertices[1].position = Vector3.Zero;
                    vertices[1].uv = new(rect.left / (float)sprite.texture.Width, rect.top / (float)sprite.texture.Height);
                    vertices[2].position = new(size.X, 0, 0);
                    vertices[2].uv = new(rect.right / (float)sprite.texture.Width, rect.top / (float)sprite.texture.Height);
                    vertices[3].position = new(size.X, size.Y, 0);
                    vertices[3].uv = new(rect.right / (float)sprite.texture.Width, rect.bottom / (float)sprite.texture.Height);

                    vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

                    indexBuffer = IndexBuffer.CreateTransient(indices);
                }

                break;

            case SpriteRenderMode.Sliced:

                {
                    if(ninePatchVertices.Length != SpriteRenderSystem.NinePatchVertexCount)
                    {
                        Array.Resize(ref ninePatchVertices, SpriteRenderSystem.NinePatchVertexCount);
                        Array.Resize(ref ninePatchIndices, SpriteRenderSystem.NinePatchVertexCount);
                    }

                    if((lastSprite != sprite || lastSize != size) && sprite.IsValid)
                    {
                        lastSprite = sprite;
                        lastSize = size;

                        var actualSize = size;

                        actualSize.X -= sprite.Border.left + sprite.Border.right;
                        actualSize.Y -= sprite.Border.top + sprite.Border.bottom;

                        SpriteRenderSystem.MakeNinePatchGeometry(ninePatchVertices.AsSpan(), ninePatchIndices.AsSpan(), sprite.texture, actualSize, sprite.Border, true);
                    }

                    if(sprite?.IsValid ?? false)
                    {
                        position += sprite.Border.Position;
                    }

                    if(ninePatchVertices.Length > 0)
                    {
                        vertexBuffer = VertexBuffer.CreateTransient(ninePatchVertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

                        indexBuffer = IndexBuffer.CreateTransient(ninePatchIndices);

                        vertexCount = ninePatchVertices.Length;
                        indexCount = ninePatchIndices.Length;
                    }
                }

                break;
        }

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertexCount, 0, indexCount, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            viewID, SetMaterial);
    }
}
