using Staple.Internal;
using System;
using System.Text.Json.Serialization;

namespace Staple;

[Serializable]
public sealed class Sprite
{
    public Texture texture;

    public int spriteIndex;

    [JsonIgnore]
    public Rect Rect => IsValid ? texture.metadata.sprites[spriteIndex].rect : default;

    [JsonIgnore]
    public Rect Border => IsValid ? texture.metadata.border : default;

    [JsonIgnore]
    public RectFloat RectFloat
    {
        get
        {
            if(IsValid == false)
            {
                return default;
            }

            var rect = texture.metadata.sprites[spriteIndex].rect;

            return new(rect.left / (float)texture.Width, rect.right / (float)texture.Width,
                rect.top / (float)texture.Height, rect.bottom / (float)texture.Height);
        }
    }

    [JsonIgnore]
    public TextureSpriteRotation Rotation => IsValid ? texture.metadata.sprites[spriteIndex].rotation : TextureSpriteRotation.None;

    [JsonIgnore]
    public bool IsValid => texture != null && texture.Disposed == false && spriteIndex >= 0 && spriteIndex < texture.metadata.sprites.Count;
}
