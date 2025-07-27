using System.Collections.Generic;
using System.Numerics;

namespace Staple.UI;

public class UIWindow(UIManager manager, string ID) : UIPanel(manager, ID)
{
    protected Texture backgroundTexture;
    protected Texture closeButtonTexture;
    protected Rect textureRect;
    protected int padding;
    protected bool dragging;
    protected Color titleFontColor;
    protected int titleFontSize;
    protected Vector2Int titlePosition;
    protected Vector2Int closeButtonPosition;
    protected Vector2 lastMousePosition;

    public string title;

    private bool isClosed;

    public bool IsClosed
    {
        get => isClosed;

        set
        {
            isClosed = value;

            Visible = !value;
        }
    }

    public int TitleHeight { get; private set; }

    public int TitleOffset {  get; private set; }

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("Window", "BackgroundTexture");
        closeButtonTexture = skin.GetTexture("Window", "CloseButtonTexture");
        textureRect = skin.GetRect("Window", "TextureRect");
        padding = skin.GetInt("Window", "Padding");
        titleFontColor = skin.GetColor("Window", "TitleFontColor");
        titleFontSize = skin.GetInt("Window", "TitleFontSize");
        titlePosition = skin.GetVector2Int("Window", "TitlePosition");
        closeButtonPosition = skin.GetVector2Int("Window", "CloseButtonPosition");
        TitleHeight = skin.GetInt("Window", "TitleBarHeight");
        TitleOffset = skin.GetInt("Window", "TitleBarOffset");

        ChildOffset = new Vector2Int(0, TitleOffset + TitleHeight);
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        //TODO
    }

    protected override void OnClick()
    {
        var position = Position - new Vector2Int(padding, 0) + GlobalPosition;

        var size = Size + new Vector2Int(padding * 2, textureRect.top);

        var min = position + new Vector2Int(size.X - closeButtonTexture.Size.X - closeButtonPosition.X, closeButtonPosition.Y);

        var max = min + closeButtonTexture.Size;

        var aabb = AABB.CreateFromMinMax(new(min, 0), new(max, 0));

        if(aabb.Contains(new(Input.PointerPosition, 0)))
        {
            IsClosed = true;
        }
    }

    public override void Update(Vector2Int parentPosition)
    {
        if(IsClosed)
        {
            return;
        }

        var position = parentPosition + Position;

        if(Input.GetMouseButton(MouseButton.Left) == false)
        {
            dragging = false;
        }
        else if(Input.GetMouseButtonDown(MouseButton.Left) &&
            Manager.FocusedElement == this)
        {
            var size = Size + new Vector2Int(padding * 2, textureRect.top);

            var min = position + new Vector2Int(0, TitleOffset);
            var max = min + new Vector2Int(size.X, TitleHeight);

            var aabb = AABB.CreateFromMinMax(new(min, 0), new(max, 0));

            if(aabb.Contains(new(Input.PointerPosition, 0)))
            {
                dragging = true;

                lastMousePosition = Input.PointerPosition;
            }
        }
        else if(dragging)
        {
            var difference = (Vector2Int)(Input.PointerPosition - lastMousePosition);

            position += difference;
            Position += difference;

            if(Manager.CurrentMenuBar != null && GlobalPosition.Y + Position.Y < 30)
            {
                Position = new(Position.X, 30);

                position.Y = 30;
            }

            var x = GlobalPosition.X + Position.X;

            if(x < 0)
            {
                Position = new(-x, Position.Y);

                position.X = -x;
            }

            lastMousePosition = Input.PointerPosition;
        }

        foreach(var child in Children)
        {
            child.Update(position + ChildOffset);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        var size = Size + new Vector2Int(padding * 2, textureRect.top);

        DrawSpriteSliced(position - new Vector2Int(padding, 0), size, backgroundTexture, textureRect, Color.White);

        RenderText(title, new TextParameters()
            .TextColor(titleFontColor)
            .Position(position + titlePosition - new Vector2Int(padding, 0)));

        DrawSprite(position +
            new Vector2Int(size.X - closeButtonTexture.Width - closeButtonPosition.X - padding, closeButtonPosition.Y),
            closeButtonTexture.Size, closeButtonTexture, Color.White);

        foreach(var child in Children)
        {
            child.Draw(position + ChildOffset);
        }
    }
}
