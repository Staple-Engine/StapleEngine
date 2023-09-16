using System.Collections.Generic;

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

        private Dictionary<int, TextResourceInfo> textResources = new();

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

                    resource.info = parameters.font.LoadGlyph(resource.character, parameters);

                    if((resource.info?.pixels?.Length ?? 0) > 0)
                    {
                        resource.sourceTexture = Texture.CreateStandard("", resource.info.pixels, StandardTextureColorComponents.RGBA);
                    }

                    textResources.Add(key, resource);
                }
            }
        }

        public void DrawText(string text, TextParameters parameters)
        {
            //TODO
        }
    }
}
