using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.UI;

public abstract class UIPanel
{
    internal UIManager Manager { get; set; }

    public bool Visible { get; set; }

    public bool Enabled { get; set; }

    public bool AllowMouseInput { get; set; }

    public bool AllowKeyboardInput { get; set; }

    public Vector2Int Position { get; set; }

    public Vector2Int Size { get; set; }

    public Vector2Int Translation { get; set; }

    public Vector2Int SelectBoxExtraSize { get; set; }

    public float Alpha { get; set; }

    public bool ShouldRespondToTooltips {  get; set; }

    public string Tooltip { get; set; }

    internal UIPanel parent;

    public UIPanel Parent
    {
        get => parent;

        set
        {
            parent?.RemoveChild(this);

            parent = value;

            parent?.AddChild(this);
        }
    }

    internal HashSet<UIPanel> children = [];

    public IEnumerable<UIPanel> Children => children;

    internal float clickTimer;

    internal bool clickPressed;

    internal string ID;

    private SpriteRenderSystem.SpriteVertex[] ninePatchVertices = [];
    private uint[] ninePatchIndices = [];

    private static Material material;

    private static readonly SpriteRenderSystem.SpriteVertex[] vertices = new SpriteRenderSystem.SpriteVertex[4];

    private static readonly ushort[] indices = [0, 1, 2, 2, 3, 0];

    private TextRenderer.PosTexVertex[] textVertices = [];
    private ushort[] textIndices = [];

    public bool BlockingInput { get; protected set; }

    public Action<UIPanel> OnClick;

    public Action<UIPanel> OnLoseFocus;

    public Action<UIPanel> OnGainFocus;

    public Action<UIPanel, char> OnCharacterEntered;

    public Action<UIPanel> OnMouseMove;

    public Action<UIPanel, MouseButton> OnMouseJustPressed;

    public Action<UIPanel, MouseButton> OnMousePressed;

    public Action<UIPanel, MouseButton> OnMouseReleased;

    public Action<UIPanel, KeyCode> OnKeyJustPressed;

    public Action<UIPanel, KeyCode> OnKeyPressed;

    public Action<UIPanel, KeyCode> OnKeyReleased;

    public Vector2Int ParentPosition
    {
        get
        {
            if(parent == null)
            {
                return Vector2Int.Zero;
            }

            var position = Vector2Int.Zero;

            var p = parent;

            while(p != null)
            {
                position += p.Position;

                p = p.parent;
            }

            return position;
        }
    }

    public Vector2Int ChildrenSize
    {
        get
        {
            var outValue = Vector2Int.Zero;

            foreach(var child in children)
            {
                if(child.Visible == false)
                {
                    continue;
                }

                child.PerformLayout();

                var max = child.Position + child.Size;

                if(outValue.X < max.X)
                {
                    outValue.X = max.X;
                }

                if (outValue.Y < max.Y)
                {
                    outValue.Y = max.Y;
                }
            }

            return outValue;
        }
    }

    public UIPanel(UIManager manager)
    {
        Manager = manager;

        Enabled = AllowMouseInput = AllowKeyboardInput = Visible = true;

        Alpha = 1;
    }

    protected bool IsCulled(Vector2Int position)
    {
        return Visible == false ||
            Alpha == 0 ||
            position.X + Size.X < 0 ||
            position.Y + Size.Y < 0 ||
            position.X > Manager.CanvasSize.X ||
            position.Y > Manager.CanvasSize.Y;
    }

    private void AddChild(UIPanel child)
    {
        if (child == null)
        {
            return;
        }

        child.parent?.RemoveChild(child);

        children.Add(child);

        if(child.parent != this)
        {
            child.parent = this;
        }
    }

    private void RemoveChild(UIPanel child)
    {
        if (child != null)
        {
            child.parent = null;
        }

        children.Remove(child);
    }

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

        OnMouseJustPressed?.Invoke(this, button);
    }

    internal void OnMousePressedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMousePressed?.Invoke(this, button);
    }

    internal void OnMouseReleasedInternal(MouseButton button)
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        if(button == MouseButton.Left)
        {
            clickPressed = false;

            if(Time.time - clickTimer < 0.5f)
            {
                OnClick?.Invoke(this);
            }
        }

        OnMouseReleased?.Invoke(this, button);
    }

    internal void OnMouseMoveInternal()
    {
        if (AllowMouseInput == false || Enabled == false)
        {
            return;
        }

        OnMouseMove?.Invoke(this);
    }

    internal void OnKeyJustPressedInternal(KeyCode key)
    {
        if(AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyJustPressed?.Invoke(this, key);
    }

    internal void OnKeyPressedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyPressed?.Invoke(this, key);
    }

    internal void OnKeyReleasedInternal(KeyCode key)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnKeyReleased?.Invoke(this, key);
    }

    internal void OnCharacterEnteredInternal(char character)
    {
        if (AllowKeyboardInput == false || Enabled == false)
        {
            return;
        }

        OnCharacterEntered?.Invoke(this, character);
    }

    internal void OnLoseFocusInternal()
    {
        OnLoseFocus?.Invoke(this);
    }

    internal void OnGainFocusInternal()
    {
        OnGainFocus?.Invoke(this);
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

        material.shader.SetColor(material.GetShaderHandle(Material.MainColorProperty), color);
        material.shader.SetTexture(material.GetShaderHandle(Material.MainTextureProperty), texture);

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, 4, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            Manager.ViewID);
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

        if(vertexBuffer == null || indexBuffer == null)
        {
            return;
        }

        material.shader.SetColor(material.GetShaderHandle(Material.MainColorProperty), color);
        material.shader.SetTexture(material.GetShaderHandle(Material.MainTextureProperty), texture);

        material.DisableShaderKeyword(Shader.SkinningKeyword);
        material.DisableShaderKeyword(Shader.InstancingKeyword);

        Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, 4, 0, 6, material, Vector3.Zero,
            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0)), MeshTopology.Triangles, MaterialLighting.Unlit,
            Manager.ViewID);
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
            Matrix4x4.CreateTranslation(new Vector3(parameters.position.X, parameters.position.Y, 0)),
            MeshTopology.Triangles, MaterialLighting.Unlit, Manager.ViewID);
    }

    public bool RespondsToTooltips() => ShouldRespondToTooltips && string.IsNullOrEmpty(Tooltip);

    public virtual void PerformLayout()
    {
        foreach (var child in children)
        {
            child.PerformLayout();
        }
    }

    public abstract void SetSkin(UISkin skin);

    public abstract void Update(Vector2Int parentPosition);

    public abstract void Draw(Vector2Int parentPosition);
}
