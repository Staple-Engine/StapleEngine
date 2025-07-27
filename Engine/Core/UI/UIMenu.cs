using System;
using System.Collections.Generic;

namespace Staple.UI;

public class UIMenu(UIManager manager) : UIPanel(manager)
{
    private Texture backgroundTexture;
    private Texture selectorBackgroundTexture;
    private Vector2Int padding;
    private Vector2Int textOffset;
    private int selectorPadding;
    private int itemHeight;
    private Rect textureRect;
    private int fontSize;
    private Color fontColor;

    private List<Item> items = [];

    public class Item(string caption, object userData, int index)
    {
        public readonly string caption = caption;

        public readonly object userData = userData;

        internal readonly int index = index;
    }

    public Action<Item> OnItemSelected;

    public void AddItem(string caption, object userData = null)
    {
        items.Add(new(caption, userData, items.Count));
    }

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("Menu", "BackgroundTexture");
        selectorBackgroundTexture = skin.GetTexture("Menu", "SelectorBackgroundTexture");
        padding = skin.GetVector2Int("Menu", "Padding");
        textOffset = skin.GetVector2Int("Menu", "TextOffset");
        selectorPadding = skin.GetInt("Menu", "SelectorPadding");
        itemHeight = skin.GetInt("Menu", "ItemHeight");
        textureRect = skin.GetRect("Menu", "TextureRect");
        fontSize = Manager.DefaultFontSize;
        fontColor = Manager.DefaultFontColor;
    }

    protected override void PerformLayout()
    {
        var w = Size.X;

        for (var i = 0; i < items.Count; i++)
        {
            var textWidth = MeasureTextSimple(items[i].caption, new TextParameters()
                .FontSize(fontSize)).AbsoluteSize.X;

            if(textWidth > w)
            {
                w = textWidth;
            }
        }

        Size = new(w, items.Count * itemHeight + padding.Y * 2);
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position - new Vector2Int(padding.X, 0);

        if(IsCulled(position))
        {
            return;
        }

        var size = Size + new Vector2Int(4 + padding.X, padding.Y);

        DrawSpriteSliced(position, size, backgroundTexture, textureRect, Color.White);

        var pointerPosition = Input.PointerPosition;

        var drewSelector = false;

        for(int i = 0, yPos = 0; i < items.Count; i++, yPos += itemHeight)
        {
            var min = position + new Vector2Int(padding.X, padding.Y + yPos);
            var max = min + new Vector2Int(Size.X, itemHeight);

            var aabb = AABB.CreateFromMinMax(new(min, 0), new(max, 0));

            if(drewSelector == false && aabb.Contains(new(pointerPosition, 0)))
            {
                drewSelector = true;

                DrawSprite(min, new Vector2Int(size.X, itemHeight + selectorPadding), selectorBackgroundTexture, Color.White);
            }

            var textSize = MeasureTextSimple(items[i].caption, new TextParameters()
                .FontSize(fontSize)).AbsoluteSize;

            RenderText(items[i].caption, new TextParameters()
                .FontSize(fontSize)
                .TextColor(fontColor)
                .Position(min + new Vector2Int(0, (itemHeight - textSize.Y) / 2)));
        }
    }
}
