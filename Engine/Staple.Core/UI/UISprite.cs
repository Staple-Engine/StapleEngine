using Staple.Internal;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UISprite(UIManager manager, string ID) : UIPanel(manager, ID)
{
    public Sprite sprite;

    public override void SetSkin(UISkin skin)
    {
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(sprite), out var t) &&
                t is JsonElement p &&
                (p.ValueKind == JsonValueKind.String))
            {
                var pieces = p.GetString().Split(':');

                if (pieces.Length != 2 ||
                    int.TryParse(pieces[1], out var spriteIndex) == false ||
                    spriteIndex < 0)
                {
                    return;
                }

                var guid = AssetDatabase.GetAssetGuid(pieces[0]) ?? pieces[0];

                var texture = ResourceManager.instance.LoadTexture(guid);

                if(texture != null && spriteIndex < texture.Sprites.Length)
                {
                    sprite = texture.Sprites[spriteIndex];
                }
            }
        }
    }

    protected override void PerformLayout()
    {
    }

    public override void Update(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        foreach (var child in Children)
        {
            child.Update(position);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = GlobalPosition + Position;

        if(sprite == null || IsCulled(position))
        {
            return;
        }

        DrawSprite(position, Size, sprite.texture, sprite.Rect, Color.White.WithAlpha(Alpha));

        foreach(var child in Children)
        {
            child.Draw(position);
        }
    }
}
