using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Internal;

[MessagePackObject]
public class FontMetadata
{
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    [Tooltip("Expected font sizes used by this font.\nUsed to validate the texture size in the asset importer.")]
    public List<int> expectedSizes = new();

    [Key(2)]
    public FontCharacterSet includedCharacterSets = FontCharacterSet.BasicLatin;

    [Key(3)]
    public int textureSize = 512;

    [Key(4)]
    public bool useAntiAliasing = true;

    [Key(5)]
    public string typeName = typeof(FontAsset).FullName;

    public static bool operator ==(FontMetadata lhs, FontMetadata rhs)
    {
        if (lhs is null && rhs is null)
        {
            return true;
        }

        if (lhs is null || rhs is null)
        {
            return false;
        }

        return lhs.guid == rhs.guid &&
            lhs.expectedSizes.SequenceEqual(rhs.expectedSizes) &&
            lhs.includedCharacterSets == rhs.includedCharacterSets &&
            lhs.textureSize == rhs.textureSize &&
            lhs.useAntiAliasing == rhs.useAntiAliasing &&
            lhs.typeName == rhs.typeName;
    }

    public static bool operator !=(FontMetadata lhs, FontMetadata rhs)
    {
        if (lhs is null && rhs is null)
        {
            return false;
        }

        if (lhs is null || rhs is null)
        {
            return true;
        }

        return lhs.guid != rhs.guid ||
            lhs.expectedSizes.SequenceEqual(rhs.expectedSizes) == false ||
            lhs.includedCharacterSets != rhs.includedCharacterSets ||
            lhs.textureSize != rhs.textureSize ||
            lhs.useAntiAliasing != rhs.useAntiAliasing ||
            lhs.typeName != rhs.typeName;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is FontMetadata rhs)
        {
            return this == rhs;
        }

        return false;
    }
}
