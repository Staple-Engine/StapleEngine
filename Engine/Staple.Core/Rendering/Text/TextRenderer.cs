using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

public class TextRenderer
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct PosTexVertex
    {
        public Vector2 position;
        public Vector2 uv;
    }

    private TextFont defaultFont;

    private TextFont DefaultFont
    {
        get
        {
            if(defaultFont == null)
            {
                LoadDefaultFont();
            }

            return defaultFont;
        }
    }

    public static Lazy<VertexLayout> VertexLayout = new(() => VertexLayoutBuilder.CreateNew()
        .Add(VertexAttribute.Position, VertexAttributeType.Float2)
        .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float2)
        .Build());

    public static readonly TextRenderer instance = new();

    public Texture FontTexture(TextParameters parameters)
    {
        var font = ResourceManager.instance.LoadFont(parameters.font)?.font ?? DefaultFont;

        if (font == null)
        {
            return null;
        }

        font.TextColor = parameters.textColor;
        font.SecondaryTextColor = parameters.secondaryTextColor;
        font.BorderSize = parameters.borderSize;
        font.BorderColor = parameters.borderColor;

        //Trigger texture generation
        font.FontSize = parameters.fontSize;

        return font.Texture;
    }

    public void LoadDefaultFont()
    {
        var data = Convert.FromBase64String(FontData.IntelOneMonoRegular);

        defaultFont = TextFont.FromData(data, "DEFAULT", true, 1024, FontCharacterSet.BasicLatin |
            FontCharacterSet.Latin1Supplement |
            FontCharacterSet.LatinExtendedA |
            FontCharacterSet.LatinExtendedB);

        if(defaultFont != null)
        {
            Log.Debug($"[TextRenderer] Loaded default font");
        }
    }

    public Rect MeasureTextSimple(string str, TextParameters parameters)
    {
        if (str == null || str.Length == 0)
        {
            return default;
        }

        var font = ResourceManager.instance.LoadFont(parameters.font)?.font ?? DefaultFont;

        if (font == null)
        {
            return default;
        }

        var lineSpacing = font.LineSpacing(parameters);
        var spaceSize = (int)(parameters.fontSize * (2 / 3.0f));

        var position = new Vector2Int(0, parameters.fontSize);

        var min = Vector2Int.Zero;
        var max = Vector2Int.Zero;

        var lines = str.Replace("\r", "").Split('\n');
        var first = true;

        for(var i = 0; i < lines.Length; i++)
        {
            for(var j = 0; j < lines[i].Length; j++)
            {
                var glyph = font.GetGlyph(lines[i][j]);

                if(glyph == null)
                {
                    position.X += spaceSize;

                    continue;
                }

                if(first)
                {
                    first = false;

                    min.X = glyph.bounds.left;
                    min.Y = glyph.bounds.top;

                    max.X = glyph.bounds.right;
                    max.Y = position.Y - glyph.bounds.bottom;
                }

                switch (lines[i][j])
                {
                    case ' ':

                        position.X += spaceSize;

                        break;

                    default:

                        if(j > 0)
                        {
                            position.X += font.Kerning(lines[i][j - 1], lines[i][j], parameters);
                        }

                        position.X += glyph.xAdvance;

                        break;
                }
            }

            if(position.X < min.X)
            {
                min.X = position.X;
            }

            if(position.X > max.X)
            {
                max.X = position.X;
            }

            if (position.Y < min.Y)
            {
                min.Y = position.Y;
            }

            if (position.Y > max.Y)
            {
                max.Y = position.Y;
            }

            position.X = 0;
            position.Y += lineSpacing;
        }

        return new Rect(min.X, max.X, min.Y, max.Y);
    }

    public void FitTextAroundLength(string str, TextParameters parameters, float lengthInPixels, out int fontSize)
    {
        if(lengthInPixels <= 0)
        {
            fontSize = 0;

            return;
        }

        parameters = parameters.Clone();

        fontSize = parameters.fontSize;

        while(MeasureTextSimple(str, parameters.FontSize(fontSize)).right > lengthInPixels)
        {
            fontSize--;
        }
    }

    public string[] FitTextOnRect(string str, TextParameters parameters, Vector2Int rectSize)
    {
        var lines = new List<string>();

        var offset = 0;
        var previousOffset = 0;

        var fragments = new List<string>();

        for (; ; )
        {
            previousOffset = offset;

            var matchSpace = str.IndexOf(' ', previousOffset);
            var matchNewLine = str.IndexOf('\n', previousOffset);

            var match = -1;

            if (matchSpace >= 0)
            {
                match = matchSpace;
            }

            if (matchNewLine >= 0 && (matchSpace == -1 || matchNewLine < matchSpace))
            {
                match = matchNewLine;
            }

            if (match < 0)
            {
                if (offset < str.Length)
                {
                    fragments.Add(str.Substring(offset));
                }

                break;
            }

            if (match - previousOffset > 0)
            {
                fragments.Add(str.Substring(previousOffset, match - previousOffset));

                if (match == matchNewLine)
                {
                    fragments.Add("\n");
                }
            }
            else if (match == matchNewLine)
            {
                fragments.Add("\n");
            }

            offset = match + 1;
        }

        var currentSize = Vector2Int.Zero;

        var builder = new StringBuilder();
        var builder2 = new StringBuilder();

        var first = true;

        var currentText = new StringBuilder();

        var newLineIndex = -1;

        while (currentSize.Y < rectSize.Y)
        {
            if (first == false)
            {
                builder.Append(' ');
            }

            if (first)
            {
                first = false;
            }

            if (fragments.Count > 0 && fragments[0][0] != '\n')
            {
                builder.Append(fragments[0]);
            }

            if (fragments.Count > 0 && ((newLineIndex = fragments[0].IndexOf('\n')) == 0 || newLineIndex == 1))
            {
                if (first == false)
                {
                    var s = builder.ToString().Substring(0, builder.Length - 1);

                    builder.Clear();
                    builder.Append(s);
                }

                lines.Add(builder.ToString());

                builder.Append('\n');

                currentText.Append(builder);

                builder.Clear();
                builder2.Clear();

                fragments.RemoveAt(0);

                first = true;

                continue;
            }

            currentSize = MeasureTextSimple(currentText + builder.ToString(), parameters).AbsoluteSize;

            //Early out
            if (currentSize.Y > rectSize.Y)
            {
                return lines.ToArray();
            }

            if (currentSize.X > rectSize.X)
            {
                if (builder2.Length > 0)
                {
                    builder.Clear();
                    builder.Append(builder2);
                }

                currentSize = MeasureTextSimple(builder.ToString(), parameters).AbsoluteSize;

                //So by default it would exceed the size
                if (currentSize.X > rectSize.X)
                {
                    return lines.ToArray();
                }

                //Verify the old text
                currentSize = MeasureTextSimple(currentText + builder.ToString(), parameters).AbsoluteSize;

                var ignoreNewline = currentText.Length > 0 && currentText[currentText.Length - 1] == '\n';

                if (ignoreNewline == false)
                {
                    //Remove extra space
                    if (first == false && currentText.Length > 0)
                    {
                        var s = currentText.ToString().Substring(0, currentText.Length - 1);

                        currentText.Clear();
                        currentText.Append(s);
                    }

                    currentText.Append('\n');
                }

                //If we added text, add that line to our lines and reset everything

                if (builder.Length > 0)
                {
                    lines.Add(builder.ToString());
                }

                builder.Clear();
                builder2.Clear();

                first = true;

                continue;
            }

            //Save the last working text here
            if (fragments.Count > 0)
            {
                builder2.Clear();
                builder2.Append(builder);

                fragments.RemoveAt(0);
            }
            //Final check here
            else
            {
                currentSize = MeasureTextSimple(currentText + builder.ToString(), parameters).AbsoluteSize;

                if (currentSize.X > rectSize.X || currentSize.Y > rectSize.Y)
                {
                    return lines.ToArray();
                }

                lines.Add(builder.ToString());

                return lines.ToArray();
            }
        }

        if (builder.Length > 0)
        {
            lines.Add(builder.ToString());
        }

        return lines.ToArray();
    }

    public Rect MeasureTextLines(IEnumerable<string> lines, TextParameters parameters)
    {
        //TODO: Update to new logic
        var outValue = new Rect();

        var additionalBottom = 0;

        var y = 0;
        var first = true;

        foreach(var line in lines)
        {
            var temp = MeasureTextSimple(line, parameters);

            if(first)
            {
                outValue = temp;
            }

            //Compensate for extra space due to lower letters like y and p
            if(temp.bottom > parameters.fontSize)
            {
                additionalBottom += temp.bottom - parameters.fontSize;
            }

            temp.top += y;
            temp.bottom += y;

            if(temp.left < outValue.left)
            {
                outValue.left = temp.left;
            }

            if(temp.top < outValue.top)
            {
                outValue.top = temp.top;
            }

            if(temp.right > outValue.right)
            {
                outValue.right = temp.right;
            }

            if(temp.bottom > outValue.bottom)
            {
                outValue.bottom = temp.bottom;
            }

            y += parameters.fontSize;
        }

        outValue.bottom += additionalBottom;

        return outValue;
    }

    public void DrawText(string text, Matrix4x4 transform, TextParameters parameters, Material material, float scale, bool flipY, ushort viewID)
    {
        if(text == null)
        {
            throw new ArgumentNullException("text");
        }

        if(material == null)
        {
            throw new ArgumentNullException("material");
        }

        var font = ResourceManager.instance.LoadFont(parameters.font)?.font ?? DefaultFont;

        if (font == null)
        {
            return;
        }

        font.TextColor = parameters.textColor;
        font.SecondaryTextColor = parameters.secondaryTextColor;
        font.BorderSize = parameters.borderSize;
        font.BorderColor = parameters.borderColor;
        font.FontSize = parameters.fontSize;

        if (MakeTextGeometry(text, parameters, scale, flipY, out var vertices, out var indices))
        {
            /*
            if(VertexBuffer.TransientBufferHasSpace(vertices.Length, VertexLayout.Value) &&
                IndexBuffer.TransientBufferHasSpace(indices.Length, false))
            {
                var vertexBuffer = VertexBuffer.CreateTransient(vertices.AsSpan(), VertexLayout.Value);
                var indexBuffer = IndexBuffer.CreateTransient(indices);

                if(vertexBuffer == null || indexBuffer == null)
                {
                    return;
                }

                material.MainTexture = font.Texture;

                Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertices.Length, 0, indices.Length,
                    material, Vector3.Zero, transform, MeshTopology.Triangles, MaterialLighting.Unlit, viewID);
            }
            */
        }
    }

    public bool MakeTextGeometry(string text, TextParameters parameters, float scale, bool flipY,
        out PosTexVertex[] vertices, out ushort[] indices)
    {
        ArgumentNullException.ThrowIfNull(text);

        var font = ResourceManager.instance.LoadFont(parameters.font)?.font ?? DefaultFont;

        if (font == null)
        {
            vertices = default;
            indices = default;

            return false;
        }

        font.TextColor = parameters.textColor;
        font.SecondaryTextColor = parameters.secondaryTextColor;
        font.BorderSize = parameters.borderSize;
        font.BorderColor = parameters.borderColor;
        font.FontSize = parameters.fontSize;

        var lineSpace = font.LineSpacing(parameters) * scale;
        var spaceSize = parameters.fontSize * 2 / 3.0f * scale;

        var position = new Vector2(parameters.position.X, parameters.position.Y);

        var initialPosition = position;

        var lines = text.Replace("\r", "").Split('\n');

        var outVertices = new List<PosTexVertex>();
        var outIndices = new List<ushort>();

        foreach (var line in lines)
        {
            for (var j = 0; j < line.Length; j++)
            {
                switch (line[j])
                {
                    case ' ':

                        position.X += spaceSize;

                        break;

                    default:

                        if (j > 0)
                        {
                            position.X += font.Kerning(line[j - 1], line[j], parameters) * scale;
                        }

                        var glyph = font.GetGlyph(line[j]);

                        if (glyph != null)
                        {
                            var size = new Vector2(glyph.bounds.Width * scale, glyph.bounds.Height * scale);

                            var advance = glyph.xAdvance * scale;

                            var yOffset = flipY ? -glyph.yOffset : (glyph.yOffset - glyph.bounds.Height);

                            var p = position + new Vector2(glyph.xOffset * scale, yOffset * scale);

                            outIndices.Add((ushort)outVertices.Count);
                            outIndices.Add((ushort)(outVertices.Count + 1));
                            outIndices.Add((ushort)(outVertices.Count + 2));
                            outIndices.Add((ushort)(outVertices.Count + 2));
                            outIndices.Add((ushort)(outVertices.Count + 3));
                            outIndices.Add((ushort)(outVertices.Count));

                            if (flipY)
                            {
                                outVertices.Add(new()
                                {
                                    position = p + new Vector2(0, size.Y),
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.bottom)
                                });

                                outVertices.Add(new()
                                {
                                    position = p,
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.top)
                                });

                                outVertices.Add(new()
                                {
                                    position = p + new Vector2(size.X, 0),
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.top)
                                });

                                outVertices.Add(new()
                                {
                                    position = p + size,
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.bottom)
                                });
                            }
                            else
                            {
                                outVertices.Add(new()
                                {
                                    position = p,
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.bottom)
                                });

                                outVertices.Add(new()
                                {
                                    position = p + new Vector2(0, size.Y),
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.top)
                                });

                                outVertices.Add(new()
                                {
                                    position = p + size,
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.top)
                                });

                                outVertices.Add(new()
                                {
                                    position = p + new Vector2(size.X, 0),
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.bottom)
                                });
                            }

                            position.X += advance;
                        }
                        else
                        {
                            position.X += spaceSize;
                        }

                        break;
                }
            }

            position.X = initialPosition.X;
            position.Y += lineSpace;
        }

        vertices = outVertices.ToArray();
        indices = outIndices.ToArray();

        return true;
    }

    public bool MakeTextGeometry(string text, TextParameters parameters, float scale, bool flipY,
        ref PosTexVertex[] vertices, ref ushort[] indices, out int vertexCount, out int indexCount)
    {
        ArgumentNullException.ThrowIfNull(text);

        vertexCount = 0;
        indexCount = 0;

        var font = ResourceManager.instance.LoadFont(parameters.font)?.font ?? DefaultFont;

        if (font == null)
        {
            vertices = default;
            indices = default;

            return false;
        }

        font.TextColor = parameters.textColor;
        font.SecondaryTextColor = parameters.secondaryTextColor;
        font.BorderSize = parameters.borderSize;
        font.BorderColor = parameters.borderColor;
        font.FontSize = parameters.fontSize;

        var lineSpace = font.LineSpacing(parameters) * scale;
        var spaceSize = parameters.fontSize * 2 / 3.0f * scale;

        var position = new Vector2(parameters.position.X, parameters.position.Y);

        var initialPosition = position;

        var lines = text.Replace("\r", "").Split('\n');

        //First: Get count

        foreach (var line in lines)
        {
            for (var j = 0; j < line.Length; j++)
            {
                switch (line[j])
                {
                    case ' ':

                        break;

                    default:

                        var glyph = font.GetGlyph(line[j]);

                        if (glyph != null)
                        {
                            indexCount += 6;
                            vertexCount += 4;
                        }

                        break;
                }
            }
        }

        //Second: Fill

        if(vertexCount > vertices.Length)
        {
            Array.Resize(ref vertices, vertexCount);
        }

        if (indexCount > indices.Length)
        {
            Array.Resize(ref indices, indexCount);
        }

        var vertexCounter = 0;
        var indexCounter = 0;

        foreach (var line in lines)
        {
            for (var j = 0; j < line.Length; j++)
            {
                switch (line[j])
                {
                    case ' ':

                        position.X += spaceSize;

                        break;

                    default:

                        if (j > 0)
                        {
                            position.X += font.Kerning(line[j - 1], line[j], parameters) * scale;
                        }

                        var glyph = font.GetGlyph(line[j]);

                        if (glyph != null)
                        {
                            var size = new Vector2(glyph.bounds.Width * scale, glyph.bounds.Height * scale);

                            var advance = glyph.xAdvance * scale;

                            var yOffset = flipY ? -glyph.yOffset : (glyph.yOffset - glyph.bounds.Height);

                            var p = position + new Vector2(glyph.xOffset * scale, yOffset * scale);

                            indices[indexCounter++] = (ushort)vertexCounter;
                            indices[indexCounter++] = (ushort)(vertexCounter + 1);
                            indices[indexCounter++] = (ushort)(vertexCounter + 2);
                            indices[indexCounter++] = (ushort)(vertexCounter + 2);
                            indices[indexCounter++] = (ushort)(vertexCounter + 3);
                            indices[indexCounter++] = (ushort)vertexCounter;

                            if (flipY)
                            {
                                vertices[vertexCounter++] = new()
                                {
                                    position = p + new Vector2(0, size.Y),
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.bottom)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p,
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.top)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p + new Vector2(size.X, 0),
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.top)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p + size,
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.bottom)
                                };
                            }
                            else
                            {
                                vertices[vertexCounter++] = new()
                                {
                                    position = p,
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.bottom)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p + new Vector2(0, size.Y),
                                    uv = new Vector2(glyph.uvBounds.left, glyph.uvBounds.top)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p + size,
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.top)
                                };

                                vertices[vertexCounter++] = new()
                                {
                                    position = p + new Vector2(size.X, 0),
                                    uv = new Vector2(glyph.uvBounds.right, glyph.uvBounds.bottom)
                                };
                            }

                            position.X += advance;
                        }
                        else
                        {
                            position.X += spaceSize;
                        }

                        break;
                }
            }

            position.X = initialPosition.X;
            position.Y += lineSpace;
        }

        return true;
    }
}
