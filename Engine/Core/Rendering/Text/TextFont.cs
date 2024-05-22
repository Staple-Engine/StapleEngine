using StbTrueTypeSharp;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class TextFont : IDisposable
{
    public class FontAtlasInfo
    {
        public int fontSize;
        public FontCharacterSet ranges;
        public Texture atlas;
        public int ascent, descent, lineGap;

        public Dictionary<int, Glyph> glyphs = new();

        public Dictionary<Vector2Int, int> kerning = new();
    }

    internal static readonly Dictionary<FontCharacterSet, (int, int)> characterRanges = new()
    {
        { FontCharacterSet.BasicLatin, (0x0020, 0x007F) },
        { FontCharacterSet.Latin1Supplement, (0x00A0, 0x00FF) },
        { FontCharacterSet.LatinExtendedA, (0x0100, 0x017F) },
        { FontCharacterSet.LatinExtendedB, (0x0180, 0x024F) },
        { FontCharacterSet.Cyrillic, (0x0400, 0x04FF) },
        { FontCharacterSet.CyrillicSupplement, (0x0500, 0x052F) },
        { FontCharacterSet.Hiragana, (0x3040, 0x309F) },
        { FontCharacterSet.Katakana, (0x30A0, 0x30FF) },
        { FontCharacterSet.Greek, (0x0370, 0x03FF) },
        { FontCharacterSet.CjkSymbolsAndPunctuation, (0x3000, 0x303F) },
        { FontCharacterSet.CjkUnifiedIdeographs, (0x4E00, 0x9FFF) },
        { FontCharacterSet.HangulCompatibilityJamo, (0x3130, 0x318F) },
        { FontCharacterSet.HangulSyllables, (0xAC00, 0xD7AF) },
    };

    internal const FontCharacterSet AllCharacterSets = FontCharacterSet.BasicLatin |
        FontCharacterSet.CjkSymbolsAndPunctuation |
        FontCharacterSet.CjkUnifiedIdeographs |
        FontCharacterSet.Cyrillic |
        FontCharacterSet.CyrillicSupplement |
        FontCharacterSet.Greek |
        FontCharacterSet.HangulCompatibilityJamo |
        FontCharacterSet.HangulSyllables |
        FontCharacterSet.Hiragana |
        FontCharacterSet.Katakana |
        FontCharacterSet.Latin1Supplement |
        FontCharacterSet.LatinExtendedA |
        FontCharacterSet.LatinExtendedB;

    internal int fontSize;
    internal int textureSize;
    internal byte[] fontData;

    internal FontCharacterSet includedRanges;

    internal Dictionary<int, FontAtlasInfo> atlas = new();

    public int FontSize
    {
        get => fontSize;

        set
        {
            if(value <= 0)
            {
                return;
            }

            fontSize = value;

            GenerateTextureAtlas();
        }
    }

    public Texture Texture => atlas.TryGetValue(fontSize, out var info) ? info.atlas : null;

    public bool MakePixelData(int fontSize, int textureSize, out byte[] bitmapData, out int ascent, out int descent,
        out int lineGap, out Dictionary<int, Glyph> glyphs)
    {
        using var font = StbTrueType.CreateFont(fontData, 0);

        if (font == null || fontSize <= 0 || textureSize <= 0)
        {
            bitmapData = default;
            glyphs = default;
            ascent = default;
            descent = default;
            lineGap = default;

            return false;
        }

        var monoBitmap = new byte[textureSize * textureSize];

        var context = new StbTrueType.stbtt_pack_context();

        unsafe
        {
            fixed (byte* ptr = monoBitmap)
            {
                StbTrueType.stbtt_PackBegin(context, ptr, textureSize, textureSize, textureSize, 1, null);
            }
        }

        var scaleFactor = StbTrueType.stbtt_ScaleForPixelHeight(font, fontSize);

        int a, b, c;

        unsafe
        {
            StbTrueType.stbtt_GetFontVMetrics(font, &a, &b, &c);
        }

        ascent = a;
        descent = b;
        lineGap = c;

        StbTrueType.stbtt_PackSetOversampling(context, 2, 2);

        glyphs = new Dictionary<int, Glyph>();

        var values = Enum.GetValues<FontCharacterSet>();

        foreach (var value in values)
        {
            if (includedRanges.HasFlag(value) == false ||
                characterRanges.TryGetValue(value, out var range) == false ||
                range.Item1 > range.Item2)
            {
                continue;
            }

            var chars = new StbTrueType.stbtt_packedchar[range.Item2 - range.Item1 + 1];

            unsafe
            {
                fixed (StbTrueType.stbtt_packedchar* charPtr = chars)
                {
                    StbTrueType.stbtt_PackFontRange(context, font.data, 0, fontSize, range.Item1, chars.Length, charPtr);
                }
            }

            for (var i = 0; i < chars.Length; i++)
            {
                var chr = chars[i];

                var yOffset = chr.yoff;

                yOffset += ascent * scaleFactor;

                var glyph = new Glyph()
                {
                    bounds = new Rect(chr.x0, chr.x1, chr.y0, chr.y1),
                    xOffset = (int)chr.xoff,
                    yOffset = Math.RoundToInt(yOffset),
                    xAdvance = Math.RoundToInt(chr.xadvance),
                };

                glyphs.Add(range.Item1 + i, glyph);
            }
        }

        StbTrueType.stbtt_PackEnd(context);

        var rgbBitmap = new byte[textureSize * textureSize * 4];

        for (int i = 0, localIndex = 0; i < monoBitmap.Length; i++, localIndex += 4)
        {
            rgbBitmap[localIndex] = monoBitmap[i];
            rgbBitmap[localIndex + 1] = monoBitmap[i];
            rgbBitmap[localIndex + 2] = monoBitmap[i];
            rgbBitmap[localIndex + 3] = monoBitmap[i];
        }

        bitmapData = rgbBitmap;

        return true;
    }

    public void GenerateTextureAtlas()
    {
        if(atlas.ContainsKey(FontSize))
        {
            return;
        }

        if(MakePixelData(FontSize, textureSize, out var rgbBitmap, out var ascent, out var descent, out var lineGap, out var glyphs) == false)
        {
            return;
        }

        var texture = Texture.CreatePixels("", rgbBitmap, (ushort)textureSize, (ushort)textureSize, new()
        {
            filter = TextureFilter.Point,
            type = TextureType.Texture,
            useMipmaps = false,
        }, Bgfx.bgfx.TextureFormat.RGBA8);

        if(texture == null)
        {
            return;
        }

        atlas.Add(FontSize, new()
        {
            ascent = ascent,
            atlas = texture,
            descent = descent,
            fontSize = fontSize,
            glyphs = glyphs,
            lineGap = lineGap,
            ranges = includedRanges,
        });
    }

    public void Clear()
    {
        foreach(var pair in atlas)
        {
            pair.Value.atlas.Destroy();
        }

        atlas.Clear();
    }

    public int LineSpacing(TextParameters parameters)
    {
        FontSize = parameters.fontSize;

        if (atlas.TryGetValue(fontSize, out var info) == false)
        {
            return FontSize;
        }

        return info.lineGap;
    }

    public int Kerning(char from, char to, TextParameters parameters)
    {
        if(atlas.TryGetValue(fontSize, out var info) == false ||
            info.kerning.TryGetValue(new(from, to), out var kerning) == false)
        {
            return 0;
        }

        return kerning;
    }

    public void Dispose()
    {
        Clear();
    }

    public Glyph GetGlyph(int codepoint)
    {
        return atlas.TryGetValue(fontSize, out var info) && info.glyphs.TryGetValue(codepoint, out var glyph) ? glyph : new();
    }

    public static TextFont FromData(byte[] data, int textureSize = 1024, FontCharacterSet ranges = AllCharacterSets)
    {
        using var font = StbTrueType.CreateFont(data, 0);

        if(font == null)
        {
            return null;
        }

        var outValue = new TextFont()
        {
            fontData = data,
            textureSize = textureSize,
            includedRanges = ranges,
        };

        outValue.FontSize = 32;

        return outValue;
    }
}
