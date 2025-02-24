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
    public TextureSpriteRotation Rotation => IsValid ? texture.metadata.sprites[spriteIndex].rotation : TextureSpriteRotation.None;

    [JsonIgnore]
    public bool IsValid => texture != null && texture.Disposed == false && spriteIndex >= 0 && spriteIndex < texture.metadata.sprites.Count;
}
