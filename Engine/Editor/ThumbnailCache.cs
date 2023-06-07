using Staple.Internal;
using System.Collections.Generic;

namespace Staple.Editor
{
    internal class ThumbnailCache
    {
        private static Dictionary<string, Texture> cachedThumbnails = new();

        public static Texture GetThumbnail(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var texture))
            {
                return texture;
            }

            try
            {
                texture = Texture.CreateStandard(path, System.IO.File.ReadAllBytes(path), StandardTextureColorComponents.RGBA);
            }
            catch(System.Exception)
            {
                return null;
            }

            if(texture != null)
            {
                cachedThumbnails.Add(path, texture);
            }

            return texture;
        }

        public static void Clear()
        {
            foreach(var pair in cachedThumbnails)
            {
                pair.Value.Destroy();
            }

            cachedThumbnails.Clear();
        }
    }
}
