using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.UI;

public partial class UIPanel
{
    /// <summary>
    /// Draws a sprite in the UI
    /// </summary>
    /// <param name="position">The position of the sprite</param>
    /// <param name="size">The size in pixels of the sprite</param>
    /// <param name="texture">The texture to use</param>
    /// <param name="color">The color to use</param>
    protected void DrawSprite(Vector2Int position, Vector2Int size, Texture texture, Color color)
    {
        if (texture?.Disposed ?? true)
        {
            return;
        }

        material ??= new(SpriteRenderSystem.DefaultMaterial.Value);

        vertices[0].position = new(0, size.Y, 0);
        vertices[0].uv = new(0, 1);
        vertices[1].position = Vector3.Zero;
        vertices[1].uv = new(0, 0);
        vertices[2].position = new(size.X, 0, 0);
        vertices[2].uv = new(1, 0);
        vertices[3].position = new(size.X, size.Y, 0);
        vertices[3].uv = new(1, 1);

        var vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(indices);

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        var c = material.MainColor;
        var t = material.MainTexture;

        material.MainColor = color;
        material.MainTexture = texture;

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, 4, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            Manager.ViewID);

        material.MainColor = c;
        material.MainTexture = t;
    }

    /// <summary>
    /// Draws a sprite with a specific area in the UI
    /// </summary>
    /// <param name="position">The position of the sprite</param>
    /// <param name="size">The size in pixels of the sprite</param>
    /// <param name="texture">The texture to use</param>
    /// <param name="rect">The sprite area in pixels</param>
    /// <param name="color">The color to use</param>
    protected void DrawSprite(Vector2Int position, Vector2Int size, Texture texture, Rect rect, Color color)
    {
        if (texture?.Disposed ?? true)
        {
            return;
        }

        material ??= new(SpriteRenderSystem.DefaultMaterial.Value);

        vertices[0].position = new(0, size.Y, 0);
        vertices[0].uv = new(rect.left / (float)texture.Width, rect.bottom / (float)texture.Height);
        vertices[1].position = Vector3.Zero;
        vertices[1].uv = new(rect.left / (float)texture.Width, rect.top / (float)texture.Height);
        vertices[2].position = new(size.X, 0, 0);
        vertices[2].uv = new(rect.right / (float)texture.Width, rect.top / (float)texture.Height);
        vertices[3].position = new(size.X, size.Y, 0);
        vertices[3].uv = new(rect.right / (float)texture.Width, rect.bottom / (float)texture.Height);

        var vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(indices);

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        var c = material.MainColor;
        var t = material.MainTexture;

        material.MainColor = color;
        material.MainTexture = texture;

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, 4, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            Manager.ViewID);

        material.MainColor = c;
        material.MainTexture = t;
    }

    /// <summary>
    /// Draws a sliced sprite to the UI
    /// </summary>
    /// <param name="position">The position of the sprite</param>
    /// <param name="size">The size of the sprite</param>
    /// <param name="texture">The texture to use</param>
    /// <param name="border">The slice border of the sprite</param>
    /// <param name="color">The color to use</param>
    protected void DrawSpriteSliced(Vector2Int position, Vector2Int size, Texture texture, Rect border, Color color)
    {
        if (texture?.Disposed ?? true)
        {
            return;
        }

        if (ninePatchVertices.Length != SpriteRenderSystem.NinePatchVertexCount)
        {
            Array.Resize(ref ninePatchVertices, SpriteRenderSystem.NinePatchVertexCount);
            Array.Resize(ref ninePatchIndices, SpriteRenderSystem.NinePatchVertexCount);
        }

        var actualSize = size;

        actualSize.X -= border.left + border.right;
        actualSize.Y -= border.top + border.bottom;

        SpriteRenderSystem.MakeNinePatchGeometry(ninePatchVertices.AsSpan(), ninePatchIndices.AsSpan(), texture, actualSize, border, true);

        position += border.Position;

        var vertexBuffer = VertexBuffer.CreateTransient(ninePatchVertices.AsSpan(), SpriteRenderSystem.vertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(ninePatchIndices);

        material ??= new(SpriteRenderSystem.DefaultMaterial.Value);

        if (vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        var c = material.MainColor;
        var t = material.MainTexture;

        material.MainColor = color;
        material.MainTexture = texture;

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, ninePatchVertices.Length, 0, ninePatchIndices.Length, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            Manager.ViewID);

        material.MainColor = c;
        material.MainTexture = t;
    }

    /// <summary>
    /// Measures text without taking word wrapping into account
    /// </summary>
    /// <param name="str">The text to measure</param>
    /// <param name="parameters">Text parameters for the text</param>
    /// <returns>The measured text rectangle</returns>
    protected Rect MeasureTextSimple(string str, TextParameters parameters) => TextRenderer.instance.MeasureTextSimple(str, parameters);

    /// <summary>
    /// Fits text around a specific length in pixels
    /// </summary>
    /// <param name="str">The text</param>
    /// <param name="parameters">Text parameters for the text</param>
    /// <param name="lengthInPixels">The total length (width) of pixels</param>
    /// <param name="fontSize">The expected font size</param>
    protected void FitTextAroundLength(string str, TextParameters parameters, float lengthInPixels, out int fontSize) =>
        TextRenderer.instance.FitTextAroundLength(str, parameters, lengthInPixels, out fontSize);

    /// <summary>
    /// Fits text within a rectangle
    /// </summary>
    /// <param name="str">The text</param>
    /// <param name="parameters">Text parameters for the text</param>
    /// <param name="rectSize">The size of the rectangle</param>
    /// <returns>The text split into strings</returns>
    protected string[] FitTextOnRect(string str, TextParameters parameters, Vector2Int rectSize) =>
        TextRenderer.instance.FitTextOnRect(str, parameters, rectSize);

    /// <summary>
    /// Renders text to the UI
    /// </summary>
    /// <param name="str">The text</param>
    /// <param name="parameters">Text parameters for the text</param>
    protected void RenderText(string str, TextParameters parameters)
    {
        material ??= new(SpriteRenderSystem.DefaultMaterial.Value);

        parameters.Position(parameters.position + new Vector2(0, parameters.fontSize));

        if (TextRenderer.instance.MakeTextGeometry(str, parameters, 1, true, ref textVertices, ref textIndices,
            out var vertexCount, out var indexCount) == false)
        {
            return;
        }

        var texture = TextRenderer.instance.FontTexture(parameters);

        if (texture == null)
        {
            return;
        }

        var vertexBuffer = VertexBuffer.CreateTransient(textVertices.AsSpan(), TextRenderer.VertexLayout.Value);

        var indexBuffer = IndexBuffer.CreateTransient(textIndices);

        material.MainTexture = texture;

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertexCount, 0, indexCount, material, Vector3.Zero,
            Matrix4x4.Identity, MeshTopology.Triangles, MaterialLighting.Unlit, Manager.ViewID);
    }
}
