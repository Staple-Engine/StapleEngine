using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

[Flags]
public enum FontCharacterSet
{
    BasicLatin = (1 << 1),
    Latin1Supplement = (1 << 2),
    LatinExtendedA = (1 << 3),
    LatinExtendedB = (1 << 4),
    Cyrillic = (1 << 5),
    CyrillicSupplement = (1 << 6),
    Hiragana = (1 << 7),
    Katakana = (1 << 8),
    Greek = (1 << 9),
    CjkSymbolsAndPunctuation = (1 << 10),
    CjkUnifiedIdeographs = (1 << 11),
    HangulCompatibilityJamo = (1 << 12),
    HangulSyllables = (1 << 13),
}

[MessagePackObject]
public class SerializableFontHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'F', 'N', 'T'
    };

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class FontGlyphInfo
{
    [Key(0)]
    public int codepoint;

    [Key(1)]
    public int xAdvance;

    [Key(2)]
    public int xOffset;

    [Key(3)]
    public int yOffset;

    [Key(4)]
    public Vector4Holder bounds;
}

[MessagePackObject]
public class SerializableFont
{
    [Key(0)]
    public FontMetadata metadata;

    [Key(1)]
    public byte[] fontData;
}
