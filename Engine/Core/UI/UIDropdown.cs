using System;
using System.Collections.Generic;

namespace Staple.UI;

public class UIDropdown(UIManager manager, string ID) : UIPanel(manager, ID)
{
    private Texture backgroundTexture;
    private Texture dropdownTexture;
    private Vector2Int textOffset;
    private Vector2Int dropdownOffset;
    private int dropdownHeight;
    private Rect textureRect;

    public int SelectedIndex = -1;

    public readonly List<string> items = [];

    public int fontSize = 16;

    public Action<UIDropdown> OnItemClicked;

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("Dropdown", "BackgroundTexture");
        dropdownTexture = skin.GetTexture("Dropdown", "DropdownTexture");
        textOffset = skin.GetVector2Int("Dropdown", "TextOffset");
        dropdownOffset = skin.GetVector2Int("Dropdown", "Offset");
        dropdownHeight = skin.GetInt("Dropdown", "Height");
        textureRect = skin.GetRect("Dropdown", "TextureRect");
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        //TODO
    }

    protected override void PerformLayout()
    {
        Size = new(Size.X, dropdownHeight);
    }

    private void SetSelectedItem(UIMenu.Item item)
    {
        SelectedIndex = item.index;

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

        DrawSpriteSliced(position, new(Size.X, dropdownHeight), backgroundTexture, textureRect, Color.White);

        var itemName = SelectedIndex < 0 || SelectedIndex >= items.Count ? "(None)" : items[SelectedIndex];

        RenderText(itemName, new TextParameters()
            .FontSize(fontSize)
            .Position(position + textOffset));

        DrawSprite(position + new Vector2Int(Size.X - dropdownOffset.X - dropdownTexture.Size.X, dropdownOffset.Y),
            dropdownTexture.Size, dropdownTexture, textureRect, Color.White);
    }
}
