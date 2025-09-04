using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Staple.Internal;

[JsonConverter(typeof(JsonStringEnumConverter<TextureMetadataQuality>))]
public enum TextureMetadataQuality
{
    Default,
    Fastest,
    Highest
}

[JsonConverter(typeof(JsonStringEnumConverter<TextureMetadataFormat>))]
public enum TextureMetadataFormat
{
    R1,
    A8,
    R8,
    R8I,
    R8U,
    R8S,
    R16,
    R16I,
    R16U,
    R16F,
    R16S,
    R32I,
    R32U,
    R32F,
    RG8,
    RG8I,
    RG8U,
    RG8S,
    RG16,
    RG16I,
    RG16U,
    RG16F,
    RG16S,
    RG32I,
    RG32U,
    RG32F,
    RGB8,
    RGB8I,
    RGB8U,
    RGB8S,
    RGB9E5,
    BGRA8,
    RGBA8,
    RGBA8I,
    RGBA8U,
    RGBA8S,
    RGBA16,
    RGBA16I,
    RGBA16U,
    RGBA16F,
    RGBA16S,
    RGBA32I,
    RGBA32U,
    RGBA32F,
    R5G6B5,
    RGBA4,
    RGB5A1,
    RGB10A2,
    RG11B10F,
    D16,
    D24,
    D24S8,
    D32,
    D16F,
    D24F,
    D32F,
    D0S8,
    BC1,
    BC2,
    BC3,
    BC4,
    BC5,
    BC6H,
    BC7,
    ETC1,
    ETC2,
    ETC2A,
    ETC2A1,
    PTC12,
    PTC14,
    PTC12A,
    PTC14A,
    PTC22,
    PTC24,
    ATC,
    ATCE,
    ATCI,
    ASTC4x4,
    ASTC5x5,
    ASTC6x6,
    ASTC8x5,
    ASTC8x6,
    ASTC10x5,
}

[JsonConverter(typeof(JsonStringEnumConverter<TextureType>))]
public enum TextureType
{
    Texture,
    NormalMap,
    Sprite,
}

[JsonConverter(typeof(JsonStringEnumConverter<TextureWrap>))]
public enum TextureWrap
{
    Repeat,
    Clamp,
    Mirror,
}

[JsonConverter(typeof(JsonStringEnumConverter<TextureFilter>))]
public enum TextureFilter
{
    Point,
    Linear,
    Anisotropic
}

[JsonConverter(typeof(JsonStringEnumConverter<SpriteTextureMethod>))]
public enum SpriteTextureMethod
{
    Single,
    Grid,
    Custom,
}

[MessagePackObject]
public class TextureMetadataOverride
{
    [Key(0)]
    public bool shouldOverride = false;

    [Key(1)]
    public TextureMetadataFormat format = TextureMetadataFormat.RGBA8;

    [Key(2)]
    public TextureMetadataQuality quality = TextureMetadataQuality.Default;

    [Key(3)]
    public int maxSize = 2048;

    [Key(4)]
    public bool premultiplyAlpha = false;

    public static bool operator==(TextureMetadataOverride lhs, TextureMetadataOverride rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }

        if (rhs is null)
        {
            return lhs is null;
        }

        return lhs.shouldOverride == rhs.shouldOverride &&
            lhs.format == rhs.format &&
            lhs.quality == rhs.quality &&
            lhs.maxSize == rhs.maxSize &&
            lhs.premultiplyAlpha == rhs.premultiplyAlpha;
    }

    public static bool operator !=(TextureMetadataOverride lhs, TextureMetadataOverride rhs)
    {
        if (lhs is null)
        {
            return rhs is not null;
        }

        if (rhs is null)
        {
            return lhs is not null;
        }

        return lhs.shouldOverride != rhs.shouldOverride ||
            lhs.format != rhs.format ||
            lhs.quality != rhs.quality ||
            lhs.maxSize != rhs.maxSize ||
            lhs.premultiplyAlpha != rhs.premultiplyAlpha;
    }

    public override bool Equals(object obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is TextureMetadataOverride o && this == o)
        {
            return true;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(shouldOverride,
            format,
            quality,
            maxSize,
            premultiplyAlpha);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<TextureSpriteRotation>))]
public enum TextureSpriteRotation
{
    None,
    Duplicate,
    FlipY,
    FlipX,
}

[MessagePackObject]
public class TextureSpriteInfo
{
    [Key(0)]
    public Rect rect;

    [Key(1)]
    public TextureSpriteRotation rotation;

    [Key(2)]
    public Rect originalRect;
}

[MessagePackObject]
public class TextureMetadata
{
    [IgnoreMember]
    public static readonly int[] TextureMaxSizes =
    [
        32,
        64,
        128,
        256,
        512,
        1024,
        2048,
        4096,
        8192,
        16384,
    ];

    [HideInInspector]
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    public TextureType type = TextureType.Texture;

    [Key(2)]
    public TextureMetadataFormat format = TextureMetadataFormat.BC3;

    [Key(3)]
    public TextureMetadataQuality quality = TextureMetadataQuality.Default;

    [Key(4)]
    public TextureFilter filter = TextureFilter.Linear;

    [Key(5)]
    public TextureWrap wrapU = TextureWrap.Clamp;

    [Key(6)]
    public TextureWrap wrapV = TextureWrap.Clamp;

    [Key(7)]
    public TextureWrap wrapW = TextureWrap.Clamp;

    [Key(8)]
    public bool premultiplyAlpha = false;

    [Key(9)]
    public int maxSize = 2048;

    [Key(10)]
    public bool useMipmaps = true;

    [Key(11)]
    public bool isLinear = true;

    [Key(12)]
    public int spritePixelsPerUnit = 100;

    [Tooltip("Whether the texture should be read back by the CPU (cannot be used for normal rendering)")]
    [Key(13)]
    public bool readBack = false;

    [Tooltip("Whether to keep a copy of the texture data on the CPU for retrieving later")]
    [Key(14)]
    public bool keepOnCPU = false;

    [Key(15)]
    public SpriteTextureMethod spriteTextureMethod = SpriteTextureMethod.Single;

    [Key(16)]
    public Vector2Int spriteTextureGridSize = Vector2Int.Zero;

    [HideInInspector]
    [Key(17)]
    public List<TextureSpriteInfo> sprites = [];

    [Key(18)]
    public bool shouldPack = false;

    [Key(19)]
    public int padding = 0;

    [Key(20)]
    public bool trimDuplicates = false;

    [Key(21)]
    public Rect border;

    [Key(22)]
    public Dictionary<AppPlatform, TextureMetadataOverride> overrides = new()
    {
        {
            AppPlatform.iOS, new TextureMetadataOverride()
            {
                shouldOverride = true,
                format = TextureMetadataFormat.ASTC4x4,
            }
        },
        {
            AppPlatform.Android, new TextureMetadataOverride()
            {
                shouldOverride = true,
                format = TextureMetadataFormat.ASTC4x4,
            }
        },
    };

    [HideInInspector]
    [Key(23)]
    public string typeName = typeof(Texture).FullName;

    public TextureMetadata Clone()
    {
        return new TextureMetadata()
        {
            guid = guid,
            filter = filter,
            format = format,
            isLinear = isLinear,
            maxSize = maxSize,
            overrides = new(overrides),
            premultiplyAlpha = premultiplyAlpha,
            quality = quality,
            readBack = readBack,
            keepOnCPU = keepOnCPU,
            type = type,
            useMipmaps = useMipmaps,
            wrapU = wrapU,
            wrapV = wrapV,
            wrapW = wrapW,
            sprites = new(sprites),
            spriteTextureGridSize = spriteTextureGridSize,
            spriteTextureMethod = spriteTextureMethod,
            spritePixelsPerUnit = spritePixelsPerUnit,
            shouldPack = shouldPack,
            padding = padding,
            trimDuplicates = trimDuplicates,
            typeName = typeName,
            border = border,
        };
    }

    public static bool operator==(TextureMetadata lhs, TextureMetadata rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }

        if (rhs is null)
        {
            return lhs is null;
        }

        return lhs.guid == rhs.guid &&
            lhs.type == rhs.type &&
            lhs.format == rhs.format &&
            lhs.quality == rhs.quality &&
            lhs.filter == rhs.filter &&
            lhs.wrapU == rhs.wrapU &&
            lhs.wrapV == rhs.wrapV &&
            lhs.wrapW == rhs.wrapW && 
            lhs.premultiplyAlpha == rhs.premultiplyAlpha &&
            lhs.maxSize == rhs.maxSize &&
            lhs.useMipmaps == rhs.useMipmaps &&
            lhs.isLinear == rhs.isLinear &&
            lhs.spritePixelsPerUnit == rhs.spritePixelsPerUnit &&
            lhs.readBack == rhs.readBack &&
            lhs.keepOnCPU == rhs.keepOnCPU &&
            lhs.overrides.Keys.Count == rhs.overrides.Keys.Count &&
            lhs.overrides.Keys.All(x => rhs.overrides.ContainsKey(x) && lhs.overrides[x] == rhs.overrides[x]) &&
            lhs.spriteTextureMethod == rhs.spriteTextureMethod &&
            lhs.spriteTextureGridSize == rhs.spriteTextureGridSize &&
            lhs.sprites.Count == rhs.sprites.Count &&
            lhs.sprites.SequenceEqual(rhs.sprites) &&
            lhs.shouldPack == rhs.shouldPack &&
            lhs.padding == rhs.padding &&
            lhs.trimDuplicates == rhs.trimDuplicates &&
            lhs.typeName == rhs.typeName &&
            lhs.border == rhs.border;
    }

    public static bool operator !=(TextureMetadata lhs, TextureMetadata rhs)
    {
        if (lhs is null)
        {
            return rhs is not null;
        }

        if (rhs is null)
        {
            return lhs is not null;
        }

        return lhs.guid != rhs.guid ||
            lhs.type != rhs.type ||
            lhs.format != rhs.format ||
            lhs.quality != rhs.quality ||
            lhs.filter != rhs.filter ||
            lhs.wrapU != rhs.wrapU ||
            lhs.wrapV != rhs.wrapV ||
            lhs.wrapW != rhs.wrapW ||
            lhs.premultiplyAlpha != rhs.premultiplyAlpha ||
            lhs.maxSize != rhs.maxSize ||
            lhs.useMipmaps != rhs.useMipmaps ||
            lhs.isLinear != rhs.isLinear ||
            lhs.spritePixelsPerUnit != rhs.spritePixelsPerUnit ||
            lhs.readBack != rhs.readBack ||
            lhs.keepOnCPU != rhs.keepOnCPU ||
            lhs.overrides.Keys.Count != rhs.overrides.Keys.Count &&
            lhs.overrides.Keys.Any(x => rhs.overrides.ContainsKey(x) == false || lhs.overrides[x] != rhs.overrides[x]) ||
            lhs.spriteTextureMethod != rhs.spriteTextureMethod ||
            lhs.spriteTextureGridSize != rhs.spriteTextureGridSize ||
            lhs.sprites.Count != rhs.sprites.Count ||
            lhs.sprites.SequenceEqual(rhs.sprites) == false ||
            lhs.shouldPack != rhs.shouldPack ||
            lhs.padding != rhs.padding ||
            lhs.overrides != rhs.overrides ||
            lhs.trimDuplicates != rhs.trimDuplicates ||
            lhs.typeName != rhs.typeName ||
            lhs.border != rhs.border;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if(obj is TextureMetadata rhs)
        {
            return this == rhs;
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(guid);
        hash.Add(type);
        hash.Add(format);
        hash.Add(quality);
        hash.Add(filter);
        hash.Add(wrapU);
        hash.Add(wrapV);
        hash.Add(wrapW);
        hash.Add(premultiplyAlpha);
        hash.Add(maxSize);
        hash.Add(useMipmaps);
        hash.Add(isLinear);
        hash.Add(spritePixelsPerUnit);
        hash.Add(readBack);
        hash.Add(keepOnCPU);
        hash.Add(spriteTextureMethod);
        hash.Add(spriteTextureGridSize);
        hash.Add(sprites);
        hash.Add(shouldPack);
        hash.Add(padding);
        hash.Add(trimDuplicates);
        hash.Add(overrides);
        hash.Add(typeName);
        hash.Add(border);

        return hash.ToHashCode();
    }
}

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
[JsonSerializable(typeof(Vector2Int))]
[JsonSerializable(typeof(Rect))]
[JsonSerializable(typeof(TextureSpriteInfo))]
[JsonSerializable(typeof(TextureMetadata))]
[JsonSerializable(typeof(TextureMetadataOverride))]
[JsonSerializable(typeof(Dictionary<AppPlatform, TextureMetadataOverride>))]
[JsonSerializable(typeof(JsonStringEnumConverter<AppPlatform>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureType>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureSpriteRotation>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureMetadataFormat>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureMetadataQuality>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureFilter>))]
[JsonSerializable(typeof(JsonStringEnumConverter<TextureWrap>))]
[JsonSerializable(typeof(JsonStringEnumConverter<SpriteTextureMethod>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
internal partial class TextureMetadataSerializationContext : JsonSerializerContext
{
}
