using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal
{
    internal class TextFont : IDisposable
    {
        public Dictionary<int, Glyph> glyphs = new();

        public int fontSize;
        public int lineSpacing;

        //TODO: Reimplement this natively
        /*
        public SharpFont.Library library;
        public SharpFont.Face face;
        */

        public int FontSize
        {
            get => fontSize;

            set
            {
                /*
                if(value <= 0 || face == null || library == null)
                {
                    return;
                }
                */

                //face.SetPixelSizes(0, (uint)value);

                fontSize = value;
            }
        }

        public void Clear()
        {
            /*
            face?.Dispose();

            face = null;

            library?.Dispose();

            library = null;
            */
        }

        public static TextFont FromData(byte[] data)
        {
            return null;

            /*
            var font = new TextFont();

            try
            {
                font.library = new SharpFont.Library();
            }
            catch(Exception)
            {
                return null;
            }

            font.face = font.library.NewMemoryFace(data, 0);

            if(font.face == null)
            {
                font.Clear();

                return null;
            }

            font.face.SelectCharmap(SharpFont.Encoding.Unicode);

            return font;
            */
        }

        public int LineSpacing(TextParameters parameters)
        {
            FontSize = parameters.fontSize;

            /*
            unsafe
            {
                return face.Size.Metrics.Height.Value >> 6;
            }
            */

            return FontSize;
        }

        public int Kerning(char from, char to, TextParameters parameters)
        {
            /*
            if(face.HasKerning == false)
            {
                return 0;
            }

            FontSize = parameters.fontSize;

            var fromIndex = face.GetCharIndex(from);
            var toIndex = face.GetCharIndex(to);

            var kerning = face.GetKerning(fromIndex, toIndex, SharpFont.KerningMode.Default);

            return kerning.X.Value >> 6;
            */

            return 0;
        }

        public Glyph LoadGlyph(char character, TextParameters parameters)
        {
            return null;

            /*
            var key = parameters.GetHashCode() ^ character.GetHashCode();

            if (glyphs.TryGetValue(key, out var glyph))
            {
                return glyph;
            }

            glyph = new();

            FontSize = parameters.fontSize;

            face.LoadChar(character, SharpFont.LoadFlags.ForceAutohint, SharpFont.LoadTarget.Normal);

            glyph.advance = face.Glyph.Metrics.HorizontalAdvance.ToInt32();

            var desc = face.Glyph.GetGlyph();

            var origin = new SharpFont.FTVector26Dot6();

            desc.ToBitmap(SharpFont.RenderMode.Normal, origin, true);

            var bitmapGlyph = desc.ToBitmapGlyph();

            var bitmap = bitmapGlyph.Bitmap;

            var glyphWidth = face.Glyph.Metrics.Width.ToInt32();
            var glyphHeight = face.Glyph.Metrics.Height.ToInt32();

            var xOffset = bitmapGlyph.Left;
            var yOffset = bitmapGlyph.Top;

            glyph.bounds = new Rect(new Vector2Int(xOffset, yOffset), new Vector2Int(glyphWidth, glyphHeight));

            var width = bitmap.Width;
            var height = bitmap.Rows;

            if(width > 0 && height > 0)
            {
                var pixelBuffer = new byte[width * height * 4];
                var bufferOffset = 0;

                switch(bitmap.PixelMode)
                {
                    case SharpFont.PixelMode.Mono:

                        {
                            for(var y = 0; y < height; y++)
                            {
                                for(var x = 0; x < width; x++)
                                {
                                    var index = (x + y * width) * 4 + 3;

                                    pixelBuffer[index] = (byte)((((bitmap.BufferData[bufferOffset + x / 8]) & (1 << (7 - (x % 8)))) != 0) ? 255 : 0);
                                }

                                bufferOffset += bitmap.Pitch;
                            }
                        }

                        break;

                    default:

                        //TODO: Other modes

                        {
                            for(var y = 0; y < height; y++)
                            {
                                for(var x = 0; x < width; x++)
                                {
                                    var index = (x + y * width) * 4 + 3;

                                    pixelBuffer[index] = bitmap.BufferData[bufferOffset + x];
                                }

                                bufferOffset += bitmap.Pitch;
                            }
                        }

                        break;
                }

                var byteColor = parameters.textColor * 255;
                var secondaryByteColor = parameters.secondaryTextColor * 255;

                var diff = new Vector3(Math.Clamp01(parameters.secondaryTextColor.r - parameters.textColor.r) * 255,
                    Math.Clamp01(parameters.secondaryTextColor.g - parameters.textColor.g) * 255,
                    Math.Clamp01(parameters.secondaryTextColor.b - parameters.textColor.b) * 255);

                for(var y = 0; y < height; y++)
                {
                    for(var x = 0; x < width; x++)
                    {
                        var index = (x + y * width) * 4;

                        if(parameters.textColor == parameters.secondaryTextColor)
                        {
                            pixelBuffer[index] = (byte)byteColor.r;
                            pixelBuffer[index + 1] = (byte)byteColor.g;
                            pixelBuffer[index + 2] = (byte)byteColor.b;
                        }
                        else
                        {
                            var percent = y / (float)(height - 1);

                            pixelBuffer[index] = (byte)(byteColor.r + diff.X * percent);
                            pixelBuffer[index + 1] = (byte)(byteColor.g + diff.Y * percent);
                            pixelBuffer[index + 2] = (byte)(byteColor.b + diff.X * percent);
                        }
                    }
                }

                glyph.pixels = pixelBuffer;
            }
            else
            {
                glyph.pixels = Array.Empty<byte>();
            }

            desc.Dispose();
            bitmapGlyph.Dispose();

            glyphs.Add(key, glyph);

            return glyph;
            */
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
