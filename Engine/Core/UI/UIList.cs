using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace Staple.UI;

public class UIList(UIManager manager, string ID) : UIPanel(manager, ID)
{
    private Texture selectorBackgroundTexture;
    private int fontSize;

    public readonly List<string> items = [];

    public Action<UIList, int> OnItemMouseOver;
    public Action<UIList, int> OnItemClick;

    public override void SetSkin(UISkin skin)
    {
        selectorBackgroundTexture = skin.GetTexture("Menu", "SelectorBackgroundTexture");

        fontSize = Manager.DefaultFontSize;
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(items), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.Array)
            {
                items.Clear();

                foreach(var element in p.EnumerateArray())
                {
                    if(element.ValueKind == JsonValueKind.String)
                    {
                        items.Add(element.GetString());
                    }
                }
            }
        }
    }

    protected void OnItemClickCheck(UIPanel self)
    {
        var position = GlobalPosition + Position;

        var height = 0.0f;

        var parentFrame = parent as UIScrollableFrame;

        for (var i = 0; i < items.Count; i++)
        {
            var size = MeasureTextSimple(items[i], new TextParameters().FontSize(fontSize)).AbsoluteSize;

            var sizeY = (size.Y < fontSize * 1.15f ? fontSize * 1.15f : size.Y);

            var parentOffset = parent != null ?
                new Vector2Int(parent.Size.X - (parentFrame.VerticalScrollbarVisible ? UIScrollableFrame.ScrollbarDraggableSize : 0),
                Math.CeilToInt(height + sizeY)) : new Vector2Int(0, Math.CeilToInt(height + sizeY));

            var aabb = AABB.CreateFromMinMax(new Vector3(position.X, position.Y + height, 0),
                new Vector3(position + parentOffset, 0));

            height += sizeY + 2;

            if(aabb.Contains(new(Input.PointerPosition, 0)))
            {
                OnItemClick?.Invoke(this, i);

                return;
            }
        }
    }

    protected override void PerformLayout()
    {
        var height = 0.0f;

        for (var i = 0; i < items.Count; i++)
        {
            var size = MeasureTextSimple(items[i], new TextParameters().FontSize(fontSize)).AbsoluteSize;

            var sizeY = (size.Y < fontSize * 1.15f ? fontSize * 1.15f : size.Y);

            height += sizeY + 2;
        }

        if(Size.Y < height)
        {
            Size = new(Size.X, Math.CeilToInt(height));
        }
    }

    public override void Update(Vector2Int parentPosition)
    {
        var position = GlobalPosition + Position;

        var height = 0.0f;

        var parentFrame = parent as UIScrollableFrame;

        for (var i = 0; i < items.Count; i++)
        {
            var size = MeasureTextSimple(items[i], new TextParameters().FontSize(fontSize)).AbsoluteSize;

            var sizeY = (size.Y < fontSize * 1.15f ? fontSize * 1.15f : size.Y);

            var parentOffset = parent != null ?
                new Vector2Int(parent.Size.X - (parentFrame.VerticalScrollbarVisible ? UIScrollableFrame.ScrollbarDraggableSize : 0),
                Math.CeilToInt(height + sizeY)) : new Vector2Int(0, Math.CeilToInt(height + sizeY));

            var aabb = AABB.CreateFromMinMax(new Vector3(position.X, position.Y + height, 0),
                new Vector3(position + parentOffset, 0));

            height += sizeY + 2;

            if (aabb.Contains(new(Input.PointerPosition, 0)))
            {
                OnItemMouseOver?.Invoke(this, i);

                return;
            }
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        if(parent is not UIScrollableFrame parentFrame)
        {
            return;
        }

        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        var height = 0.0f;

        var aabb = new AABB();

        for (var i = 0; i < items.Count; i++)
        {
            var size = MeasureTextSimple(items[i], new TextParameters().FontSize(fontSize)).AbsoluteSize;

            var sizeY = (size.Y < fontSize * 1.15f ? fontSize * 1.15f : size.Y);

            aabb = AABB.CreateFromMinMax(new Vector3(position.X, position.Y + height, 0),
                new Vector3(position + new Vector2Int(Size.X, Math.CeilToInt(height + sizeY)), 0));

            height += sizeY + 2;

            if (aabb.Contains(new(Input.PointerPosition, 0)))
            {
                DrawSprite(aabb.min.ToVector2(),
                    new Vector2Int(Size.X, (int)aabb.size.Y) / selectorBackgroundTexture.Size,
                    selectorBackgroundTexture, Color.White.WithAlpha(Alpha));
            }

            RenderText(items[i], new TextParameters()
                .FontSize(fontSize)
                .Position(aabb.min.ToVector2()));
        }

        Size = (Vector2Int)aabb.max.ToVector2() - position;
    }
}
