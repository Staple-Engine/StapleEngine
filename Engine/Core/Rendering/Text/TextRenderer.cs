using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal
{
    internal class TextRenderer
    {
        internal class TextResourceInfo
        {
            public char character;
            public TextParameters parameters;
            public Glyph info;

            public Texture sourceTexture;

            public int references;
        }

        private readonly Dictionary<int, TextResourceInfo> textResources = new();
        private TextFont defaultFont;

        public void LoadDefaultFont()
        {
            var data = Convert.FromBase64String(FontData.IntelOneMonoRegular);

            defaultFont = TextFont.FromData(data);

            if(defaultFont != null)
            {
                Log.Debug($"[TextRenderer] Loaded default font");
            }
        }

        public void ClearUnusedResources()
        {
            for(; ; )
            {
                var found = false;

                foreach(var pair in textResources)
                {
                    if(pair.Value.references == 0)
                    {
                        pair.Value.sourceTexture?.Destroy();

                        textResources.Remove(pair.Key);

                        found = true;

                        break;
                    }
                }

                if(found == false)
                {
                    break;
                }
            }

            foreach(var pair in textResources)
            {
                pair.Value.references = 0;
            }
        }

        public void GetText(string text, TextParameters parameters)
        {
            if(text == null || text.Length == 0)
            {
                return;
            }

            var font = parameters.font.TryGetTarget(out var f) ? f : defaultFont;

            if(font == null)
            {
                return;
            }

            for(var i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]) || text[i] == '\n' || text[i] == '\r')
                {
                    continue;
                }

                var key = parameters.GetHashCode() ^ text[i].GetHashCode();

                if(textResources.TryGetValue(key, out var resource))
                {
                    resource.references++;
                }
                else
                {
                    resource = new()
                    {
                        character = text[i],
                        parameters = parameters.Clone(),
                        references = 1
                    };

                    resource.info = font.LoadGlyph(resource.character, parameters);

                    if((resource.info?.pixels?.Length ?? 0) > 0)
                    {
                        resource.sourceTexture = Texture.CreatePixels("", resource.info.pixels,
                            (ushort)resource.info.bounds.Width, (ushort)resource.info.bounds.Height,
                            new TextureMetadata()
                            {
                                filter = TextureFilter.Linear,
                                format = TextureMetadataFormat.RGBA8,
                                type = TextureType.SRGB,
                                useMipmaps = false,
                                spritePixelsPerUnit = 100,
                            },
                            Bgfx.bgfx.TextureFormat.RGBA8);
                    }

                    textResources.Add(key, resource);
                }
            }
        }

        public Rect MeasureTextSimple(string str, TextParameters parameters)
        {
            if(str == null || str.Length == 0)
            {
                return default;
            }

            var font = parameters.font.TryGetTarget(out var f) ? f : defaultFont;

            if (font == null)
            {
                return default;
            }

            var lineSpacing = font.LineSpacing(parameters);
            var spaceSize = font.LoadGlyph(' ', parameters).advance;

            var position = new Vector2Int(0, parameters.fontSize);

            var min = Vector2Int.Zero;
            var max = Vector2Int.Zero;

            var lines = str.Replace("\r", "").Split("\n".ToCharArray());
            var first = true;

            for(var i = 0; i < lines.Length; i++)
            {
                for(var j = 0; j < lines[i].Length; j++)
                {
                    var glyph = font.LoadGlyph(lines[i][j], parameters);

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

                            position.X += glyph.advance;

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

            return new Rect(min.X, min.Y, max.X - min.X, max.Y - max.X);
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

        public Rect MeasureTextLines(IEnumerable<string> lines, TextParameters parameters)
        {
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

        public void DrawText(string text, TextParameters parameters, Material material, ushort viewID)
        {
            var mesh = Mesh.Quad;

            if(mesh.changed)
            {
                mesh.UploadMeshData();
            }

            var actualParams = parameters.font != null && parameters.font.TryGetTarget(out _) ? parameters : parameters.Font(defaultFont);

            var font = parameters.font.TryGetTarget(out var textFont) ? textFont : defaultFont;

            if(font == null)
            {
                return;
            }

            var lineSpace = font.LineSpacing(actualParams);
            var spaceSize = font.LoadGlyph(' ', actualParams).advance;

            var position = new Vector2(actualParams.position.X, actualParams.position.Y + actualParams.fontSize);

            var initialPosition = position;

            GetText(text, actualParams);

            var lines = text.Replace("\r", "").Split("\n".ToCharArray());

            foreach(var line in lines)
            {
                for(var j = 0; j < line.Length; j++)
                {
                    switch(line[j])
                    {
                        case ' ':

                            position.X += spaceSize;

                            break;

                        default:

                            if(j > 0)
                            {
                                position.X += font.Kerning(line[j - 1], line[j], actualParams);
                            }

                            if(textResources.TryGetValue(actualParams.GetHashCode() ^ line[j].GetHashCode(), out var resource))
                            {
                                var p = position + new Vector2(resource.info.bounds.left + resource.info.bounds.Width / 2, -resource.info.bounds.top + resource.info.bounds.Height / 2);

                                if(resource.sourceTexture != null)
                                {
                                    material.MainTexture = resource.sourceTexture;
                                }

                                MeshRenderSystem.DrawMesh(mesh, new Vector3(p, 0), Quaternion.Identity,
                                    new Vector3(resource.info.bounds.Width, resource.info.bounds.Height, 1), material, viewID);

                                position.X += resource.info.advance;
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
        }
    }
}
