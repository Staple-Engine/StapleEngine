using Staple.Internal;
using System;
using System.Numerics;

namespace Staple.UI;

public partial class UIPanel
{
    internal void OnMouseJustPressedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        if (button == MouseButton.Left)
        {
            clickTimer = Time.time;
            clickPressed = true;
        }

        OnMouseJustPressed(button);

        OnMouseJustPressedHandler?.Invoke(this, button);
    }

    internal void OnMousePressedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMousePressed(button);

        OnMousePressedHandler?.Invoke(this, button);
    }

    internal void OnMouseReleasedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        if (button == MouseButton.Left)
        {
            clickPressed = false;

            if (Time.time - clickTimer < 0.5f)
            {
                OnClick();

                OnClickHandler?.Invoke(this);
            }
        }

        OnMouseReleased(button);

        OnMouseReleasedHandler?.Invoke(this, button);
    }

    internal void OnMouseMoveInternal(Vector2Int position)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMouseMove(position);

        OnMouseMoveHandler?.Invoke(this, position);
    }

    internal void OnKeyJustPressedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyJustPressed(key);

        OnKeyJustPressedHandler?.Invoke(this, key);
    }

    internal void OnKeyPressedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyPressed(key);

        OnKeyPressedHandler?.Invoke(this, key);
    }

    internal void OnKeyReleasedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyReleased(key);

        OnKeyReleasedHandler?.Invoke(this, key);
    }

    internal void OnCharacterEnteredInternal(char character)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnCharacterEntered(character);

        OnCharacterEnteredHandler?.Invoke(this, character);
    }

    internal void OnLoseFocusInternal()
    {
        OnLoseFocus();

        OnLoseFocusHandler?.Invoke(this);
    }

    internal void OnGainFocusInternal()
    {
        OnGainFocus();

        OnGainFocusHandler?.Invoke(this);
    }

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

    protected Rect MeasureTextSimple(string str, TextParameters parameters) => TextRenderer.instance.MeasureTextSimple(str, parameters);

    protected void FitTextAroundLength(string str, TextParameters parameters, float lengthInPixels, out int fontSize) =>
        TextRenderer.instance.FitTextAroundLength(str, parameters, lengthInPixels, out fontSize);

    protected string[] FitTextOnRect(string str, TextParameters parameters, Vector2Int rectSize) =>
        TextRenderer.instance.FitTextOnRect(str, parameters, rectSize);

    public void RenderText(string str, TextParameters parameters)
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
