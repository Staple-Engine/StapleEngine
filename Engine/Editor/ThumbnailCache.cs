using Staple.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

internal class ThumbnailCache
{
    private class TextureInfo
    {
        public Texture texture;
        public string cachePath;
    }

    private enum RenderRequestType
    {
        Texture,
        Thumbnail,
    }

    private class RenderRequest
    {
        public string path;
        public RenderRequestType type;
        public bool persistentCache;
    }

    private const float TimeBetweenRenders = 0.25f;

    private static readonly Dictionary<string, TextureInfo> cachedThumbnails = new();
    private static readonly Dictionary<string, Texture> cachedTextures = new();
    private static readonly List<Texture> pendingDestructionTextures = new();
    private static readonly Dictionary<string, RawTextureData> cachedTextureData = new();
    private static readonly Dictionary<string, Texture> persistentTextures = new();
    private static readonly Dictionary<string, RenderRequest> pendingRenderRequests = new();

    private static float renderTimer = 0.0f;

    internal static string basePath;

    public static bool TryGetTextureData(string path, out RawTextureData textureData)
    {
        return cachedTextureData.TryGetValue(path, out textureData);
    }

    public static bool TryGetTexture(string path, out Texture texture)
    {
        return cachedTextures.TryGetValue(path, out texture);
    }

    public static Texture GetTexture(string path, bool persistentCache = false, bool force = false)
    {
        if(Path.IsPathRooted(path) == false)
        {
            var t = GetTexture(Path.Combine(basePath, path));

            return t;
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

        if(force)
        {
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

            if (texture == null)
            {
                return null;
            }

            if (persistentCache)
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

        if (pendingRenderRequests.ContainsKey(path))
        {
            return null;
        }

        pendingRenderRequests.Add(path, new RenderRequest()
        {
            path = path,
            type = RenderRequestType.Texture,
            persistentCache = persistentCache,
        });

        return null;
    }

    public static bool HasCachedThumbnail(string path)
    {
        return cachedThumbnails.TryGetValue(path, out var t) && t.texture != null && t.texture.Disposed == false;
    }

    public static Texture GetThumbnail(string path)
    {
        if(cachedThumbnails.TryGetValue(path, out var t))
        {
            return t.texture;
        }

        if(pendingRenderRequests.ContainsKey(path))
        {
            return null;
        }

        pendingRenderRequests.Add(path, new RenderRequest()
        {
            path = path,
            type = RenderRequestType.Thumbnail,
        });

        return null;
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

                            if (value != null && value.Disposed && (value.Guid?.Length ?? 0) > 0)
                            {
                                field.SetValue(component, ResourceManager.instance.LoadTexture(value.Guid));
                            }
                        }
                    }
                });
            });
        }

        renderTimer += Time.deltaTime;

        if(renderTimer >= TimeBetweenRenders && pendingRenderRequests.Count > 0)
        {
            renderTimer = 0;

            var first = pendingRenderRequests.FirstOrDefault();

            pendingRenderRequests.Remove(first.Key);

            switch(first.Value.type)
            {
                case RenderRequestType.Texture:

                    {
                        RawTextureData rawTextureData;

                        try
                        {
                            var data = File.ReadAllBytes(first.Value.path);

                            rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
                        }
                        catch (System.Exception)
                        {
                            break;
                        }

                        if (rawTextureData == null)
                        {
                            break;
                        }

                        var texture = Texture.CreatePixels(first.Value.path, rawTextureData.data, (ushort)rawTextureData.width, (ushort)rawTextureData.height, new TextureMetadata()
                        {
                            filter = TextureFilter.Point,
                            format = TextureMetadataFormat.RGBA8,
                            type = TextureType.Texture,
                            useMipmaps = false,
                        },
                        Bgfx.bgfx.TextureFormat.RGBA8);

                        if (texture == null)
                        {
                            break;
                        }

                        if (first.Value.persistentCache)
                        {
                            persistentTextures.AddOrSetKey(first.Value.path, texture);
                        }
                        else
                        {
                            if (cachedTextures.ContainsKey(first.Value.path))
                            {
                                cachedTextures[first.Value.path]?.Destroy();
                            }

                            cachedTextureData.AddOrSetKey(first.Value.path, rawTextureData);
                            cachedTextures.AddOrSetKey(first.Value.path, texture);
                        }
                    }

                    break;

                case RenderRequestType.Thumbnail:

                    {
                        var platform = Platform.CurrentPlatform;

                        if (platform.HasValue == false)
                        {
                            break;
                        }

                        RawTextureData rawTextureData;

                        try
                        {
                            var data = File.ReadAllBytes(first.Value.path);

                            rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
                        }
                        catch (System.Exception)
                        {
                            break;
                        }

                        if (rawTextureData == null)
                        {
                            break;
                        }

                        var cachePath = first.Value.path;

                        var index = first.Value.path.IndexOf("Assets");

                        if (index >= 0)
                        {
                            cachePath = Path.Combine(basePath, "Cache", "Staging", platform.Value.ToString(), first.Value.path.Substring(index + "Assets\\".Length));
                        }

                        Texture texture;

                        try
                        {
                            texture = ResourceManager.instance.LoadTexture(cachePath);
                        }
                        catch (System.Exception)
                        {
                            break;
                        }

                        if (texture != null)
                        {
                            cachedThumbnails.AddOrSetKey(first.Value.path, new TextureInfo()
                            {
                                texture = texture,
                                cachePath = cachePath
                            });

                            cachedTextureData.AddOrSetKey(first.Value.path, rawTextureData);
                        }
                    }

                    break;
            }
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
        pendingRenderRequests.Clear();
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
