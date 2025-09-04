using System;

namespace Staple.Internal;

public interface ITextFontSource : IDisposable
{
    int FontSize { get; set; }

    int LineSpacing { get; }

    bool Initialize(byte[] data);

    Glyph LoadGlyph(uint character, int fontSize, Color textColor, Color secondaryTextColor,
        int borderSize, Color borderColor);

    int Kerning(uint from, uint to);
}
