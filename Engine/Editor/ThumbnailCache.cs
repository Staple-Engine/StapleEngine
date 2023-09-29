using Staple.Internal;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor
{
    internal class ThumbnailCache
    {
        private static readonly Dictionary<string, Texture> cachedThumbnails = new();
        private static readonly Dictionary<string, Texture> cachedTextures = new();
        private static readonly List<Texture> pendingDestructionTextures = new();
        private static readonly Dictionary<string, RawTextureData> cachedTextureData = new();

        internal static string basePath;

        public static bool TryGetTextureData(string path, out RawTextureData textureData)
        {
            return cachedTextureData.TryGetValue(path, out textureData);
        }

        public static bool TryGetTexture(string path, out Texture texture)
        {
            return cachedTextures.TryGetValue(path, out texture);
        }

        public static Texture GetTexture(string path)
        {
            if(cachedTextures.TryGetValue(path, out var texture))
            {
                return texture;
            }

            RawTextureData rawTextureData;

            try
            {
                var data = File.ReadAllBytes(path);

                rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
            }
            catch (System.Exception)
            {
                return null;
            }

            if (rawTextureData == null)
            {
                return null;
            }

            texture = Texture.CreatePixels(path, rawTextureData.data, (ushort)rawTextureData.width, (ushort)rawTextureData.height, new TextureMetadata()
            {
                filter = TextureFilter.Point,
                format = TextureMetadataFormat.RGBA8,
                type = TextureType.SRGB,
                useMipmaps = false,
            }, Bgfx.bgfx.TextureFormat.RGBA8);

            if(texture == null)
            {
                return null;
            }

            if (cachedTextures.ContainsKey(path))
            {
                cachedTextures[path]?.Destroy();
            }

            cachedTextureData.AddOrSetKey(path, rawTextureData);
            cachedTextures.AddOrSetKey(path, texture);

            return texture;
        }

        public static Texture GetThumbnail(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var texture))
            {
                return texture;
            }

            var platform = Platform.CurrentPlatform;

            if(platform.HasValue == false)
            {
                return null;
            }

            RawTextureData rawTextureData;

            try
            {
                var data = File.ReadAllBytes(path);

                rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
            }
            catch(System.Exception)
            {
                return null;
            }

            if(rawTextureData == null)
            {
                return null;
            }

            var cachePath = path;

            var index = path.IndexOf("Assets");

            if(index >= 0)
            {
                cachePath = Path.Combine(basePath, "Cache", "Staging", platform.Value.ToString(), path.Substring(index + "Assets\\".Length));
            }

            try
            {
                texture = ResourceManager.instance.LoadTexture(cachePath);
            }
            catch(System.Exception)
            {
                return null;
            }

            if(texture != null)
            {
                cachedThumbnails.AddOrSetKey(path, texture);
                cachedTextureData.AddOrSetKey(path, rawTextureData);
            }

            return texture;
        }

        public static void OnFrameStart()
        {
            foreach(var texture in pendingDestructionTextures)
            {
                texture.Destroy();
            }

            pendingDestructionTextures.Clear();
        }

        public static void Clear()
        {
            foreach(var pair in cachedThumbnails)
            {
                pendingDestructionTextures.Add(pair.Value);
            }

            foreach(var pair in cachedTextures)
            {
                pendingDestructionTextures.Add(pair.Value);
            }

            cachedThumbnails.Clear();
            cachedTextureData.Clear();
        }

        public static void ClearSingle(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var texture))
            {
                cachedThumbnails.Remove(path);
                cachedTextureData.Remove(path);

                pendingDestructionTextures.Add(texture);
            }

            if(cachedTextures.TryGetValue(path, out texture))
            {
                cachedTextures.Remove(path);

                pendingDestructionTextures.Add(texture);
            }
        }
    }
}
