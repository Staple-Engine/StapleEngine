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
    public Rect Rect => IsValid ? texture.textureResource.metadata.sprites[spriteIndex].rect : default;

    [JsonIgnore]
    public Rect Border => IsValid ? texture.textureResource.metadata.border : default;

    [JsonIgnore]
    public RectFloat RectFloat
    {
        get
        {
            if(!IsValid)
            {
                return default;
            }

            var rect = texture.textureResource.metadata.sprites[spriteIndex].rect;

            return new(rect.left / (float)texture.Width, rect.right / (float)texture.Width,
                rect.top / (float)texture.Height, rect.bottom / (float)texture.Height);
        }
    }

    [JsonIgnore]
    public TextureSpriteRotation Rotation => IsValid ? texture.textureResource.metadata.sprites[spriteIndex].rotation : TextureSpriteRotation.None;

    [JsonIgnore]
    public bool IsValid => texture != null && !texture.Disposed && spriteIndex >= 0 && spriteIndex < texture.textureResource.metadata.sprites.Count;
}
