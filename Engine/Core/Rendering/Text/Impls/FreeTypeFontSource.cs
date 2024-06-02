using System.Runtime.InteropServices;

namespace Staple.Internal;

public class FreeTypeFontSource : ITextFontSource
{
    nint font;

    public int FontSize { get; set; }

    public int LineSpacing => FreeType.FreeType.LineSpacing(font, (uint)FontSize);

    public void Dispose()
    {
        if(font != nint.Zero)
        {
            FreeType.FreeType.FreeFont(font);

            font = nint.Zero;
        }
    }

    public bool Initialize(byte[] data)
    {
        unsafe
        {
            fixed(byte *d = data)
            {
                font = FreeType.FreeType.LoadFont(d, data.Length);
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

        return FreeType.FreeType.Kerning(font, from, to, (uint)FontSize);
    }

    public Glyph LoadGlyph(uint character, int fontSize, Color textColor, Color secondaryTextColor, int borderSize, Color borderColor)
    {
        if (font == nint.Zero)
        {
            return null;
        }

        var glyphPtr = FreeType.FreeType.LoadGlyph(font, character, (uint)fontSize, textColor, secondaryTextColor, borderSize, borderColor);

        if(glyphPtr == nint.Zero)
        {
            return null;
        }

        var glyphData = Marshal.PtrToStructure<FreeType.FreeType.Glyph>(glyphPtr);

        unsafe
        {
            if ((nint)glyphData.bitmap == nint.Zero || glyphData.width == 0 || glyphData.height == 0)
            {
                FreeType.FreeType.FreeGlyph(glyphPtr);

                return null;
            }

            var size = glyphData.width * glyphData.height * 4;

            var buffer = new byte[size];

            Marshal.Copy((nint)glyphData.bitmap, buffer, 0, buffer.Length);

            FreeType.FreeType.FreeGlyph(glyphPtr);

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
}
