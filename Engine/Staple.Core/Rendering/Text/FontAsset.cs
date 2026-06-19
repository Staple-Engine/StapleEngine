using Staple.Internal;
using System;

namespace Staple;

public class FontAsset : IGuidAsset
{
    internal FontAssetResource fontResource;

    public GuidHasher Guid => fontResource?.Guid ?? new();

    public int FontSize
    {
        get => fontResource?.font?.FontSize ?? 0;

        set
        {
            if(value <= 0 || fontResource?.font == null)
            {
                return;
            }

            fontResource?.font.FontSize = value;
        }
    }

    /// <summary>
    /// Internal check if a font fits in its texture size
    /// </summary>
    /// <param name="path">The path to the asset</param>
    /// <param name="metadata">The custom metadata to check</param>
    /// <returns>Whether the font can pack the characters</returns>
    internal static bool IsValid(string path, FontMetadata metadata)
    {
        try
        {
            var asset = ResourceManager.instance.LoadFont(path, true);

            if(asset == null ||
                asset.fontResource == null)
            {
                return false;
            }

            asset.fontResource.font.includedRanges = metadata.includedCharacterSets;
            asset.fontResource.font.textureSize = metadata.textureSize;

            foreach(var size in metadata.expectedSizes)
            {
                if(!asset.fontResource.font.MakePixelData(size, metadata.textureSize, out _, out _, out _))
                {
                    return false;
                }
            }

            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    public static object Create(string guid) => ResourceManager.instance.LoadFont(guid);
}
