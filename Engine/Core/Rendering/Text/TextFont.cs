using FreeTypeSharp;
using FreeTypeSharp.Native;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal
{
    internal class TextFont : IDisposable
    {
        public Dictionary<int, Glyph> glyphs = new();

        public int fontSize;
        public int lineSpacing;

        public FreeTypeLibrary library;
        public FreeTypeFaceFacade face;

        public int FontSize
        {
            get => fontSize;

            set
            {
                if(value <= 0 || face == null || library == null)
                {
                    return;
                }

                var result = FT.FT_Set_Pixel_Sizes(face.Face, 0, (uint)value);

                if (result == FT_Error.FT_Err_Ok)
                {
                    fontSize = value;
                }
            }
        }

        public void Clear()
        {
            if(face != null)
            {
                FT.FT_Done_Face(face.Face);

                face = null;
            }

            library?.Dispose();

            library = null;
        }

        public static TextFont FromData(byte[] data)
        {
            var font = new TextFont();

            try
            {
                font.library = new FreeTypeLibrary();
            }
            catch(Exception)
            {
                return null;
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            var d = handle.AddrOfPinnedObject();

            var error = FT.FT_New_Memory_Face(font.library.Native, d, data.Length, 0, out var face);

            handle.Free();

            if(error != FT_Error.FT_Err_Ok)
            {
                font.Clear();

                return null;
            }

            font.face = new FreeTypeFaceFacade(font.library, face);

            if(FT.FT_Select_Charmap(face, FT_Encoding.FT_ENCODING_UNICODE) != FT_Error.FT_Err_Ok)
            {
                font.Clear();

                return null;
            }

            return font;
        }

        public int LineSpacing(TextParameters parameters)
        {
            FontSize = parameters.fontSize;

            unsafe
            {
                return (int)(face.FaceRec->size->metrics.height.ToInt64() >> 6);
            }
        }

        public int Kerning(char from, char to, TextParameters parameters)
        {
            if(face.HasKerningFlag == false)
            {
                return 0;
            }

            FontSize = parameters.fontSize;

            var fromIndex = face.GetCharIndex(from);
            var toIndex = face.GetCharIndex(to);

            if(FT.FT_Get_Kerning(face.Face, fromIndex, toIndex, (uint)FT_Kerning_Mode.FT_KERNING_DEFAULT, out var kerning) != FT_Error.FT_Err_Ok)
            {
                return 0;
            }

            return (int)(kerning.x.ToInt64() >> 6);
        }

        public Glyph LoadGlyph(char character, TextParameters parameters)
        {
            var key = parameters.GetHashCode() ^ character.GetHashCode();

            if (glyphs.TryGetValue(key, out var glyph))
            {
                return glyph;
            }

            glyph = new();

            FontSize = parameters.fontSize;

            if(FT.FT_Load_Char(face.Face, character, FT.FT_LOAD_TARGET_LIGHT | FT.FT_LOAD_FORCE_AUTOHINT) != FT_Error.FT_Err_Ok)
            {
                return null;
            }

            unsafe
            {
                if (FT.FT_Get_Glyph((nint)face.GlyphSlot, out var aGlyph) != FT_Error.FT_Err_Ok)
                {
                    return null;
                }

                var origin = new FT_Vector();

                if(FT.FT_Glyph_To_Bitmap(ref aGlyph, FT_Render_Mode.FT_RENDER_MODE_NORMAL, ref origin, true) != FT_Error.FT_Err_Ok)
                {
                    FT.FT_Done_Glyph(aGlyph);

                    return null;
                }

                var bitmapGlyph = (FT_BitmapGlyphRec*)aGlyph;

                var bitmap = bitmapGlyph->bitmap;

                glyph.advance = (int)(bitmapGlyph->root.advance.x.ToInt64() >> 16);

                var width = (int)bitmap.width;
                var height = (int)bitmap.rows;

                if(width > 0 && height > 0)
                {
                    glyph.bounds = new Rect(bitmapGlyph->left, bitmapGlyph->left + width, bitmapGlyph->top, bitmapGlyph->top + height);

                    var pixelBuffer = new byte[width * height * 4];

                    byte* bitmapBuffer = (byte*)bitmap.buffer;

                    switch((FT_Pixel_Mode)bitmap.pixel_mode)
                    {
                        case FT_Pixel_Mode.FT_PIXEL_MODE_MONO:

                            {
                                for(var y = 0; y < height; y++)
                                {
                                    for(var x = 0; x < width; x++)
                                    {
                                        var index = (x + y * width) * 4 + 3;

                                        pixelBuffer[index] = (byte)((((bitmapBuffer[x / 8]) & (1 << (7 - (x % 8)))) != 0) ? 255 : 0);
                                    }

                                    bitmapBuffer += bitmap.pitch;
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

                                        pixelBuffer[index] = bitmapBuffer[x];
                                    }

                                    bitmapBuffer += bitmap.pitch;
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

                FT.FT_Done_Glyph(aGlyph);
            }

            glyphs.Add(key, glyph);

            return glyph;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
