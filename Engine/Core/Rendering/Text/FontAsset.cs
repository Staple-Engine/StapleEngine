using Staple.Internal;
using System;

namespace Staple;

public class FontAsset : IGuidAsset
{

    private int guidHash;
    private string guid;

    public int GuidHash => guidHash;

    public string Guid
    {
        get => guid;

        set
        {
            guid = value;

            guidHash = guid?.GetHashCode() ?? 0;
        }
    }

    internal FontMetadata metadata;

    internal TextFont font;

    public int FontSize
    {
        get => font?.FontSize ?? 0;

        set
        {
            if(value <= 0 || font == null)
            {
                return;
            }

            font.FontSize = value;
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

            if(asset == null)
            {
                return false;
            }

            asset.font.includedRanges = metadata.includedCharacterSets;
            asset.font.textureSize = metadata.textureSize;

            foreach(var size in metadata.expectedSizes)
            {
                if(asset.font.MakePixelData(size, metadata.textureSize, out _, out _, out _) == false)
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

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadFont(guid);
    }
}
