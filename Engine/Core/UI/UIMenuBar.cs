using System;
using System.Collections.Generic;

namespace Staple.UI;

public class UIMenuBar(UIManager manager, string ID) : UIPanel(manager, ID)
{
    public class Item
    {
        public string caption;
        public object userData;
        public readonly List<string> subItems = [];
    }

    private readonly List<Item> items = [];
    private Texture selectorBackgroundTexture;
    private Vector2Int padding;
    private Vector2Int textOffset;
    private int selectorPadding;
    private Rect textureRect;
    private int fontSize;
    private Color fontColor;

    public Action<string> OnMenuItemSelected;

    public override void SetSkin(UISkin skin)
    {
        selectorBackgroundTexture = skin.GetTexture("Menu", "SelectorBackgroundTexture");
        padding = skin.GetVector2Int("Menu", "Padding");
        textOffset = skin.GetVector2Int("Menu", "TextOffset");
        selectorPadding = skin.GetInt("Menu", "SelectorPadding");
        textureRect = skin.GetRect("Menu", "TextureRect");
        fontSize = Manager.DefaultFontSize;
        fontColor = Manager.DefaultFontColor;
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        //TODO
    }

    protected override void PerformLayout()
    {
        Size = new(Manager.CanvasSize.X, 25);
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        DrawSprite(Vector2Int.Zero, Size, Material.WhiteTexture, Color.Black);

        var currentPosition = Vector2Int.Zero;

        var drewSelector = false;

        var pointerPosiition = Input.PointerPosition;

        for (var i = 0; i < items.Count; i++)
        {
            var textSize = MeasureTextSimple(items[i].caption, new TextParameters()
                .FontSize(fontSize)).AbsoluteSize;

            var min = position + currentPosition + new Vector2Int(padding.X, 0);
            var max = min + new Vector2Int(textSize.X, 22);

            var aabb = AABB.CreateFromMinMax(new(min, 0), new(max, 0));

            if(drewSelector == false && aabb.Contains(new(pointerPosiition, 0)))
            {
                drewSelector = true;

                DrawSpriteSliced(min, new Vector2Int(textSize.X, 22 + selectorPadding), selectorBackgroundTexture, textureRect, Color.White);
            }

            RenderText(items[i].caption, new TextParameters()
                .FontSize(fontSize)
                .TextColor(fontColor)
                .Position(min + new Vector2Int(0, (22 - 12) / 2)));

            currentPosition.X += textSize.X + 5;
        }
    }
}
