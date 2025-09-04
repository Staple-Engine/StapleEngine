using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UIDropdown(UIManager manager, string ID) : UIPanel(manager, ID)
{
    private Texture backgroundTexture;
    private Texture dropdownTexture;
    private Vector2Int textOffset;
    private Vector2Int dropdownOffset;
    private int dropdownHeight;
    private Rect textureRect;
    private int fontSize;

    public int selectedIndex = -1;

    public readonly List<string> items = [];

    public Action<UIDropdown> OnItemClicked;

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("Dropdown", "BackgroundTexture");
        dropdownTexture = skin.GetTexture("Dropdown", "DropdownTexture");
        textOffset = skin.GetVector2Int("Dropdown", "TextOffset");
        dropdownOffset = skin.GetVector2Int("Dropdown", "Offset");
        dropdownHeight = skin.GetInt("Dropdown", "Height");
        textureRect = skin.GetRect("Dropdown", "TextureRect");

        fontSize = Manager.DefaultFontSize;
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(selectedIndex), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.Number)
            {
                selectedIndex = p.GetNumberValue<int>();
            }
        }

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

    protected override void PerformLayout()
    {
        Size = new(Size.X, dropdownHeight);
    }

    private void SetSelectedItem(UIMenu.Item item)
    {
        selectedIndex = item.index;

        OnItemClicked?.Invoke(this);
    }

    private void OnItemClickCheck()
    {

    }

    public override void Update(Vector2Int parentPosition)
    {
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        DrawSpriteSliced(position, new(Size.X, dropdownHeight), backgroundTexture, textureRect, Color.White.WithAlpha(Alpha));

        var itemName = selectedIndex < 0 || selectedIndex >= items.Count ? "(None)" : items[selectedIndex];

        RenderText(itemName, new TextParameters()
            .FontSize(fontSize)
            .Position(position + textOffset));

        DrawSprite(position + new Vector2Int(Size.X - dropdownOffset.X - dropdownTexture.Size.X, dropdownOffset.Y),
            dropdownTexture.Size, dropdownTexture, textureRect, Color.White.WithAlpha(Alpha));
    }
}
