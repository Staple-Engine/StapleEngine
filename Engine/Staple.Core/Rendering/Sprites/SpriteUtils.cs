using Staple.Internal;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

public static class SpriteUtils
{
    public const int NinePatchVertexCount = 54;

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Vertex
    {
        public Vector3 position;
        public Vector2 uv;
    }

    public static readonly Lazy<VertexLayout> VertexLayout = new(() =>
    {
        return new VertexLayoutBuilder()
            .Add(VertexAttribute.Position, 3, VertexAttributeType.Float)
            .Add(VertexAttribute.TexCoord0, 2, VertexAttributeType.Float)
            .Build();
    });

    public static readonly Lazy<Material> DefaultMaterial = new(() =>
    {
        var material = ResourceManager.instance.LoadMaterial($"Hidden/Materials/Sprite.{AssetSerialization.MaterialExtension}");

        if (material != null)
        {
            ResourceManager.instance.LockAsset(material.Guid.Guid);

            if (material.MainTexture is Texture t)
            {
                ResourceManager.instance.LockAsset(t.Guid.Guid);
            }

            if (material.shader is Shader s)
            {
                ResourceManager.instance.LockAsset(s.Guid.Guid);
            }
        }

        return material;
    });

    /// <summary>
    /// Calculates nine patch geometry for a part of the sprite 
    /// </summary>
    /// <param name="textureSize">The size of the texture</param>
    /// <param name="position">The position in the texture space</param>
    /// <param name="size">The size of the area we're generating</param>
    /// <param name="uvSize">The size of the area in UV coordinates</param>
    /// <param name="offset">The 3D offset for the piece</param>
    /// <param name="sizeOverride">If we're overriding the size, we can do so here</param>
    /// <param name="vertices">The vertices to fill in. They should have 6 elements</param>
    internal static void MakeNinePatchGeometrySlice(Vector2 textureSize, Vector2 position, Vector2 size, Vector2 uvSize,
        Vector2 offset, Vector2 sizeOverride, Span<Vertex> vertices)
    {
        if (vertices.IsEmpty ||
            vertices.Length != 6)
        {
            return;
        }

        var rect = new RectFloat(position.X, position.X + uvSize.X, position.Y, position.Y + uvSize.Y);

        var actualSize = sizeOverride.X != -1 ? sizeOverride : size;

        vertices[0].position = vertices[5].position = new Vector3(offset.X, offset.Y + actualSize.Y, 0);
        vertices[1].position = new Vector3(offset, 0);
        vertices[2].position = vertices[3].position = new Vector3(offset.X + actualSize.X, offset.Y, 0);
        vertices[4].position = new Vector3(offset + actualSize, 0);

        vertices[0].uv = vertices[5].uv = rect.Position / textureSize;
        vertices[1].uv = new Vector2(rect.left, rect.bottom) / textureSize;
        vertices[2].uv = vertices[3].uv = new Vector2(rect.right, rect.bottom) / textureSize;
        vertices[4].uv = new Vector2(rect.right, rect.top) / textureSize;
    }

    /// <summary>
    /// Calculates nine patch geometry for a sprite
    /// </summary>
    /// <param name="vertices">The vertices to update. They should have <see cref="NinePatchVertexCount"/> elements</param>
    /// <param name="indices">The indices to update. They should have <see cref="NinePatchVertexCount"/> elements</param>
    /// <param name="texture">The texture to use</param>
    /// <param name="size">The size of the sprite in world space</param>
    /// <param name="border">The nine patch border, in pixels</param>
    /// <param name="pixelCoordinates">Whether we're using world or pixel coordinates</param>
    public static void MakeNinePatchGeometry(Span<Vertex> vertices, Span<uint> indices, Texture texture, Vector2 size,
        Rect border, bool pixelCoordinates)
    {
        if ((texture?.Disposed ?? true) ||
            vertices.IsEmpty ||
            vertices.Length != NinePatchVertexCount ||
            indices.IsEmpty ||
            indices.Length != NinePatchVertexCount)
        {
            return;
        }

        var textureSize = texture.Size;
        var localScale = Vector2.Abs(size);
        var invertedLocal = new Vector2(1 / localScale.X, 1 / localScale.Y);

        if (pixelCoordinates)
        {
            invertedLocal = Vector2.One;
        }

        var fragmentPositions = new Vector2[9]
        {
            Vector2.Zero,
            new(textureSize.X - border.right, 0),
            new(0, textureSize.Y - border.bottom),
            new(textureSize.X - border.right, textureSize.Y - border.bottom),
            new(border.left, border.top),
            new(border.left, 0),
            new(border.left, textureSize.Y - border.bottom),
            new(0, border.top),
            new(textureSize.X - border.right, border.top),
        };

        var fragmentSizes = new Vector2[9]
        {
            new(border.left, border.top),
            new(border.right, border.top),
            new(border.left, border.bottom),
            new(border.right, border.bottom),
            new(textureSize.X - border.left - border.right, textureSize.Y - border.top - border.bottom),
            new(textureSize.X - border.left - border.right, border.top),
            new(textureSize.X - border.left - border.right, border.bottom),
            new(border.left, textureSize.Y - border.top - border.bottom),
            new(border.right, textureSize.Y - border.top - border.bottom),
        };

        var fragmentOffsets = new Vector2[9]
        {
            new(-border.left * invertedLocal.X, localScale.Y),
            localScale,
            new(-border.left * invertedLocal.X, -border.top * invertedLocal.Y),
            new(localScale.X, -border.top * invertedLocal.Y),
            Vector2.Zero,
            new(0, localScale.Y),
            new(0, -border.top * invertedLocal.Y),
            new(-border.left * invertedLocal.X, 0),
            new(localScale.X, 0),
        };

        var fragmentSizeOverrides = new Vector2[9]
        {
            new(border.left * invertedLocal.X, border.bottom * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.bottom * invertedLocal.Y),
            new(border.left * invertedLocal.X, border.top * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.top * invertedLocal.Y),
            localScale,
            new(localScale.X, border.bottom * invertedLocal.Y),
            new(localScale.X, border.top * invertedLocal.Y),
            new(border.left * invertedLocal.X, localScale.Y),
            new(border.right * invertedLocal.X, localScale.Y),
        };

        for (int j = 0, index = 0; j < 9; j++, index += 6)
        {
            var position = fragmentPositions[j];
            var fragmentSize = fragmentSizes[j];
            var offset = fragmentOffsets[j];
            var sizeOverride = fragmentSizeOverrides[j];

            MakeNinePatchGeometrySlice(textureSize, position, fragmentSize * texture.SpriteScale, fragmentSize, offset, sizeOverride,
                vertices.Slice(index, 6));
        }

        for (var j = 0; j < indices.Length; j++)
        {
            indices[j] = (uint)j;
        }
    }
}
