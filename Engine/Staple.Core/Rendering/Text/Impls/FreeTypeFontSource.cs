using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

public class FreeTypeFontSource : ITextFontSource
{
    nint font;

    public int FontSize { get; set; } = 0;

    public int LineSpacing => FreeType.LineSpacing(font, (uint)FontSize);

    public void Dispose()
    {
        if(font != nint.Zero)
        {
            FreeType.FreeFont(font);

            font = nint.Zero;
        }
    }

    public bool Initialize(byte[] data)
    {
        unsafe
        {
            fixed(byte *d = data)
            {
                font = FreeType.LoadFont(d, data.Length);
            }
        }

        return font != nint.Zero;
    }

    public int Kerning(uint from, uint to)
    {
        if(font == nint.Zero)
        {
            return 0;
        }

        return FreeType.Kerning(font, from, to, (uint)FontSize);
    }

    public Glyph LoadGlyph(uint character, int fontSize, Color textColor, Color secondaryTextColor, int borderSize, Color borderColor)
    {
        if (font == nint.Zero)
        {
            return Glyph.Invalid;
        }

        var glyphPtr = FreeType.LoadGlyph(font, character, (uint)fontSize, textColor, secondaryTextColor, borderSize, borderColor);

        if(glyphPtr == nint.Zero)
        {
            return Glyph.Invalid;
        }

        try
        {
            unsafe
            {
                var glyph = new Span<FreeType.Glyph>((void*)glyphPtr, 1);

                ref var glyphData = ref glyph[0];

                if ((nint)glyphData.bitmap == nint.Zero || glyphData.width == 0 || glyphData.height == 0)
                {
                    FreeType.FreeGlyph(glyphPtr);

                    glyphPtr = nint.Zero;

                    return Glyph.Invalid;
                }

                var size = glyphData.width * glyphData.height * 4;

                var buffer = new byte[size];

                new Span<byte>(glyphData.bitmap, buffer.Length).CopyTo(buffer.AsSpan());

                return new()
                {
                    bitmap = buffer,
                    bounds = new Rect(0, (int)glyphData.width, 0, (int)glyphData.height),
                    xAdvance = (int)glyphData.xAdvance,
                    xOffset = (int)glyphData.xOffset,
                    yOffset = (int)glyphData.yOffset,
                };
            }
        }
        finally
        {
            if(glyphPtr != nint.Zero)
            {
                FreeType.FreeGlyph(glyphPtr);
            }
        }
    }
}
