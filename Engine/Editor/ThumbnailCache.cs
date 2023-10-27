using Staple.Internal;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Staple.Editor
{
    internal class ThumbnailCache
    {
        private class TextureInfo
        {
            public Texture texture;
            public string cachePath;
        }

        private static readonly Dictionary<string, TextureInfo> cachedThumbnails = new();
        private static readonly Dictionary<string, Texture> cachedTextures = new();
        private static readonly List<Texture> pendingDestructionTextures = new();
        private static readonly Dictionary<string, RawTextureData> cachedTextureData = new();
        private static readonly Dictionary<string, Texture> persistentTextures = new();

        internal static string basePath;

        public static bool TryGetTextureData(string path, out RawTextureData textureData)
        {
            return cachedTextureData.TryGetValue(path, out textureData);
        }

        public static bool TryGetTexture(string path, out Texture texture)
        {
            return cachedTextures.TryGetValue(path, out texture);
        }

        public static Texture GetTexture(string path, bool persistentCache = false)
        {
            if(Path.IsPathRooted(path) == false)
            {
                var t = GetTexture(Path.Combine(basePath, path));

                if(t != null)
                {
                    return t;
                }
            }

            Texture texture;

            if(persistentCache)
            {
                if(persistentTextures.TryGetValue(path, out texture))
                {
                    return texture;
                }
            }
            else
            {
                if (cachedTextures.TryGetValue(path, out texture))
                {
                    return texture;
                }
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
                type = TextureType.Texture,
                useMipmaps = false,
            }, Bgfx.bgfx.TextureFormat.RGBA8);

            if(texture == null)
            {
                return null;
            }

            if(persistentCache)
            {
                persistentTextures.AddOrSetKey(path, texture);
            }
            else
            {
                if (cachedTextures.ContainsKey(path))
                {
                    cachedTextures[path]?.Destroy();
                }

                cachedTextureData.AddOrSetKey(path, rawTextureData);
                cachedTextures.AddOrSetKey(path, texture);
            }

            return texture;
        }

        public static Texture GetThumbnail(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var t))
            {
                return t.texture;
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

            Texture texture;

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
                cachedThumbnails.AddOrSetKey(path, new TextureInfo()
                {
                    texture = texture,
                    cachePath = cachePath
                });

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

            if (Scene.current?.world != null)
            {
                Scene.current.world.Iterate((entity) =>
                {
                    Scene.current.world.IterateComponents(entity, (ref IComponent component) =>
                    {
                        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var field in fields)
                        {
                            if (field.FieldType == typeof(Texture))
                            {
                                var value = (Texture)field.GetValue(component);

                                if (value != null && value.Disposed && (value.path?.Length ?? 0) > 0)
                                {
                                    field.SetValue(component, ResourceManager.instance.LoadTexture(value.path));
                                }
                            }
                        }
                    });
                });
            }
        }

        public static void Clear()
        {
            foreach(var pair in cachedThumbnails)
            {
                pendingDestructionTextures.Add(pair.Value.texture);
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
            if(cachedThumbnails.TryGetValue(path, out var t))
            {
                cachedThumbnails.Remove(path);
                cachedTextureData.Remove(path);

                pendingDestructionTextures.Add(t.texture);
            }

            if(cachedTextures.TryGetValue(path, out var texture))
            {
                cachedTextures.Remove(path);

                pendingDestructionTextures.Add(texture);
            }
        }
    }
}
