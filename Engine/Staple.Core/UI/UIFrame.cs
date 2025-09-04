using System.Collections.Generic;

namespace Staple.UI;

public class UIFrame(UIManager manager, string ID) : UIPanel(manager, ID)
{
    protected Texture backgroundTexture;
    protected Rect textureRect;

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("Frame", "BackgroundTexture");
        textureRect = skin.GetRect("Frame", "TextureRect");
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
    }

    public override void Update(Vector2Int parentPosition)
    {
        foreach(var child in Children)
        {
            child.Update(parentPosition + Position);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        if((backgroundTexture?.Disposed ?? true) == false)
        {
            DrawSpriteSliced(position, Size, backgroundTexture, textureRect, Color.White.WithAlpha(Alpha));
        }

        foreach (var child in Children)
        {
            if(child.Visible)
            {
                child.Draw(position);
            }
        }
    }
}
