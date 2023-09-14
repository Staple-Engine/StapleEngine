using MessagePack;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.Internal
{
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
        SRGB,
        NormalMap,
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

    [MessagePackObject]
    public class TextureMetadataOverride
    {
        [Key(0)]
        public TextureMetadataFormat format = TextureMetadataFormat.RGBA8;

        [Key(1)]
        public TextureMetadataQuality quality = TextureMetadataQuality.Default;

        [Key(2)]
        public int maxSize = 2048;

        [Key(3)]
        public bool premultiplyAlpha = false;
    }

    [MessagePackObject]
    public class TextureMetadata
    {
        [Key(0)]
        public string guid = Guid.NewGuid().ToString();

        [Key(1)]
        public TextureType type = TextureType.SRGB;

        [Key(2)]
        public TextureMetadataFormat format = TextureMetadataFormat.RGBA8;

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
        public bool isLinear = false;

        [Key(12)]
        public float spriteScale = 1;

        [Key(13)]
        public bool readBack = false;

        [Key(14)]
        public Dictionary<AppPlatform, TextureMetadataOverride> overrides = new()
        {
            {
                AppPlatform.iOS, new TextureMetadataOverride()
                {
                     format = TextureMetadataFormat.ASTC4x4,
                }
            },
            {
                AppPlatform.Android, new TextureMetadataOverride()
                {
                     format = TextureMetadataFormat.ASTC4x4,
                }
            },
        };
    }

    [JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
    [JsonSerializable(typeof(TextureMetadata))]
    [JsonSerializable(typeof(TextureMetadataOverride))]
    [JsonSerializable(typeof(Dictionary<AppPlatform, TextureMetadataOverride>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<AppPlatform>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<TextureType>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<TextureMetadataFormat>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<TextureMetadataQuality>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<TextureFilter>))]
    [JsonSerializable(typeof(JsonStringEnumConverter<TextureWrap>))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(float))]
    internal partial class TextureMetadataSerializationContext : JsonSerializerContext
    {
    }
}
