using StbTrueTypeSharp;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

public class StbTrueTypeFontSource : ITextFontSource
{
    private StbTrueType.stbtt_fontinfo font = null;
    private int fontSize = 14;
    private int lineSpacing = 0;

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

            unsafe
            {
                int ascent, descent, lineGap;

                StbTrueType.stbtt_GetFontVMetrics(font, &ascent, &descent, &lineGap);

                lineSpacing = (int)(lineGap * StbTrueType.stbtt_ScaleForPixelHeight(font, fontSize));
            }
        }
    }

    public int LineSpacing => lineSpacing;

    public void Dispose()
    {
        if(font != null)
        {
            font.Dispose();

            font = null;
        }
    }

    public bool Initialize(byte[] data)
    {
        try
        {
            font = StbTrueType.CreateFont(data, 0);
        }
        catch(Exception e)
        {
            Log.Error($"[StbTrueTypeFontSource]: Failed to load font: {e}");

            return false;
        }

        if (font == null)
        {
            return false;
        }

        return true;
    }

    public int Kerning(uint from, uint to)
    {
        return StbTrueType.stbtt_GetGlyphKernAdvance(font, (int)from, (int)to);
    }

    public Glyph LoadGlyph(uint character, int fontSize, Color textColor, Color secondaryTextColor, int borderSize, Color borderColor)
    {
        unsafe
        {
            var scale = StbTrueType.stbtt_ScaleForPixelHeight(font, fontSize);

            int width, height, xOffset, yOffset, advanceWidth, leftSideBearing;

            StbTrueType.stbtt_GetCodepointHMetrics(font, (int)character, &advanceWidth, &leftSideBearing);

            var bitmap = StbTrueType.stbtt_GetCodepointBitmap(font, 0, scale, (int)character, &width, &height, &xOffset, &yOffset);

            if(bitmap == null)
            {
                return null;
            }

            var buffer = new byte[width * height];

            Marshal.Copy((nint)bitmap, buffer, 0, buffer.Length);

            var expandedBuffer = new byte[buffer.Length * 4];

            for(int i = 0, localIndex = 0; i < buffer.Length; i++, localIndex += 4)
            {
                expandedBuffer[localIndex] = buffer[i];
                expandedBuffer[localIndex + 1] = buffer[i];
                expandedBuffer[localIndex + 2] = buffer[i];
                expandedBuffer[localIndex + 3] = buffer[i];
            }

            return new()
            {
                bitmap = expandedBuffer,
                xAdvance = advanceWidth,
                xOffset = xOffset,
                yOffset = yOffset,
                bounds = new(0, width, 0, height),
            };
        }
    }
}
