using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Staple.Editor;

/// <summary>
/// Loads and generates thumbnails for textures and other assets in the project browser
/// </summary>
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

    private static readonly Dictionary<string, TextureInfo> cachedThumbnails = new();
    private static readonly Dictionary<string, Texture> cachedTextures = new();
    private static readonly List<Texture> pendingDestructionTextures = new();
    private static readonly Dictionary<string, RawTextureData> cachedTextureData = new();
    private static readonly Dictionary<string, Texture> persistentTextures = new();

    private static readonly Dictionary<string, RenderRequest> pendingRenderRequests = new();
    private static readonly object renderRequestLock = new();

    internal static string basePath;

    /// <summary>
    /// Attempts to get the texture data for an asset
    /// </summary>
    /// <param name="path">The path of the asset</param>
    /// <param name="textureData">The asset's texture data</param>
    /// <returns>Whether it succeeded</returns>
    public static bool TryGetTextureData(string path, out RawTextureData textureData)
    {

        lock (renderRequestLock)
        {
            return cachedTextureData.TryGetValue(path, out textureData);
        }
    }

    /// <summary>
    /// Attempts to get a texture
    /// </summary>
    /// <param name="path">The texture's path</param>
    /// <param name="texture">The texture</param>
    /// <returns>Whether it succeeded</returns>
    public static bool TryGetTexture(string path, out Texture texture)
    {

        lock (renderRequestLock)
        {
            return cachedTextures.TryGetValue(path, out texture);
        }
    }

    private static void RenderTask(object state)
    {
        if(state is not RenderRequest request)
        {
            return;
        }

        void Cleanup()
        {
            pendingRenderRequests.Remove(request.path);
        }

        switch (request.type)
        {
            case RenderRequestType.Texture:

                {
                    RawTextureData rawTextureData;

                    try
                    {
                        var data = File.ReadAllBytes(request.path);

                        rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
                    }
                    catch (System.Exception)
                    {
                        Cleanup();

                        break;
                    }

                    if (rawTextureData == null)
                    {
                        Cleanup();

                        break;
                    }

                    Threading.Dispatch(() =>
                    {
                        var texture = Texture.CreatePixels(request.path, rawTextureData.data, (ushort)rawTextureData.width, (ushort)rawTextureData.height, new TextureMetadata()
                        {
                            filter = TextureFilter.Point,
                            format = TextureMetadataFormat.RGBA8,
                            type = TextureType.Texture,
                            useMipmaps = false,
                        },
                        Bgfx.bgfx.TextureFormat.RGBA8);

                        if (texture == null)
                        {
                            Cleanup();

                            return;
                        }

                        lock(renderRequestLock)
                        {
                            if (request.persistentCache)
                            {
                                persistentTextures.AddOrSetKey(request.path, texture);
                            }
                            else
                            {
                                if (cachedTextures.ContainsKey(request.path))
                                {
                                    cachedTextures[request.path]?.Destroy();
                                }

                                cachedTextureData.AddOrSetKey(request.path, rawTextureData);
                                cachedTextures.AddOrSetKey(request.path, texture);
                            }
                        }

                        Cleanup();
                    });
                }

                break;

            case RenderRequestType.Thumbnail:

                {
                    var platform = Platform.CurrentPlatform;

                    if (platform.HasValue == false)
                    {
                        Cleanup();

                        break;
                    }

                    var cachePath = request.path;

                    var index = request.path.IndexOf("Assets");

                    if (index >= 0)
                    {
                        cachePath = Path.Combine(basePath, "Cache", "Staging", platform.Value.ToString(), request.path.Substring(index + "Assets\\".Length));
                    }

                    var thumbnailPath = Path.Combine(basePath, "Cache", "Thumbnails");

                    try
                    {
                        Directory.CreateDirectory(thumbnailPath);
                    }
                    catch (Exception)
                    {
                    }

                    thumbnailPath = Path.Combine(thumbnailPath, request.path.Substring(index + "Assets\\".Length));

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath));
                    }
                    catch (Exception)
                    {
                    }

                    var lastModified = DateTime.MinValue;
                    var lastLocalModified = lastModified;

                    try
                    {
                        if (File.Exists(thumbnailPath))
                        {
                            lastModified = File.GetLastWriteTime(thumbnailPath);
                        }
                    }
                    catch (Exception)
                    {
                        lastLocalModified = DateTime.Now;
                    }

                    try
                    {
                        lastLocalModified = File.GetLastWriteTime(request.path);
                    }
                    catch (Exception)
                    {
                    }

                    RawTextureData rawTextureData;

                    if (lastLocalModified >= lastModified)
                    {
                        try
                        {
                            var data = File.ReadAllBytes(request.path);

                            rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
                        }
                        catch (Exception)
                        {
                            Cleanup();

                            break;
                        }

                        if (rawTextureData == null)
                        {
                            Cleanup();

                            break;
                        }

                        try
                        {
                            rawTextureData.Resize(64, 64);

                            var pngData = rawTextureData.EncodePNG();

                            File.WriteAllBytes(thumbnailPath, pngData);
                        }
                        catch (Exception)
                        {
                            Cleanup();

                            break;
                        }
                    }
                    else
                    {
                        try
                        {
                            var data = File.ReadAllBytes(thumbnailPath);

                            rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
                        }
                        catch (Exception)
                        {
                            Cleanup();

                            break;
                        }

                        if (rawTextureData == null)
                        {
                            Cleanup();

                            break;
                        }
                    }

                    Threading.Dispatch(() =>
                    {
                        var texture = Texture.CreatePixels(request.path, rawTextureData.data, (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                            new TextureMetadata()
                            {
                                useMipmaps = false,
                            },
                            Bgfx.bgfx.TextureFormat.RGBA8);

                        if (texture != null)
                        {
                            lock(renderRequestLock)
                            {
                                cachedThumbnails.AddOrSetKey(request.path, new TextureInfo()
                                {
                                    texture = texture,
                                    cachePath = cachePath,
                                });

                                cachedTextureData.AddOrSetKey(request.path, rawTextureData);
                            }
                        }

                        Cleanup();
                    });
                }

                break;
        }
    }

    private static void QueueRequest(string path, RenderRequest request)
    {
        lock (renderRequestLock)
        {
            if (pendingRenderRequests.ContainsKey(path))
            {
                return;
            }

            pendingRenderRequests.Add(path, request);

            ThreadPool.QueueUserWorkItem(RenderTask, request);
        }
    }

    /// <summary>
    /// Attempts to get a texture from a path
    /// </summary>
    /// <param name="path">The path of the texture</param>
    /// <param name="persistentCache">Whether to add to the persistent cache (so it won't be cleared when changing folders)</param>
    /// <param name="force">Whether to force load</param>
    /// <returns></returns>
    public static Texture GetTexture(string path, bool persistentCache = false, bool force = false)
    {
        if(Path.IsPathRooted(path) == false)
        {
            var t = GetTexture(Path.Combine(basePath, path));

            return t;
        }

        Texture texture;

        lock (renderRequestLock)
        {
            if (persistentCache)
            {
                if (persistentTextures.TryGetValue(path, out texture))
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
        }

        if(force)
        {
            RawTextureData rawTextureData;

            try
            {
                var data = File.ReadAllBytes(path);

                rawTextureData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);
            }
            catch (Exception)
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

            lock (renderRequestLock)
            {
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
            }

            return texture;
        }

        QueueRequest(path, new RenderRequest()
        {
            path = path,
            type = RenderRequestType.Texture,
            persistentCache = persistentCache,
        });

        return null;
    }

    /// <summary>
    /// Checks if there's a cached thumbnail for a path
    /// </summary>
    /// <param name="path">The path</param>
    /// <returns>Whether there's a cached thumbnail</returns>
    public static bool HasCachedThumbnail(string path)
    {
        lock(renderRequestLock)
        {
            return cachedThumbnails.TryGetValue(path, out var t) && t.texture != null && t.texture.Disposed == false;
        }
    }

    /// <summary>
    /// Attempts to get a texture for a thumbnail at a specified path
    /// </summary>
    /// <param name="path">The path</param>
    /// <returns>The texture, or null</returns>
    public static Texture GetThumbnail(string path)
    {
        var extension = Path.GetExtension(path);

        if (extension.Length == 0 ||
            AssetSerialization.TextureExtensions.Contains(extension.Substring(1)) == false)
        {
            return null;
        }

        lock (renderRequestLock)
        {
            if (cachedThumbnails.TryGetValue(path, out var t))
            {
                return t.texture;
            }

            if (pendingRenderRequests.ContainsKey(path))
            {
                return null;
            }
        }

        QueueRequest(path, new RenderRequest()
        {
            path = path,
            type = RenderRequestType.Thumbnail,
        });

        return null;
    }

    /// <summary>
    /// Called at the start of a new frame
    /// </summary>
    public static void OnFrameStart()
    {
        foreach(var texture in pendingDestructionTextures)
        {
            texture.Destroy();
        }

        pendingDestructionTextures.Clear();

        if (World.Current != null)
        {
            Scene.IterateEntities((entity) =>
            {
                entity.IterateComponents((ref IComponent component) =>
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
    }

    /// <summary>
    /// Clears all cached textures (Except persistent cache)
    /// </summary>
    public static void Clear()
    {
        lock(renderRequestLock)
        {
            foreach (var pair in cachedThumbnails)
            {
                pendingDestructionTextures.Add(pair.Value.texture);
            }

            foreach (var pair in cachedTextures)
            {
                pendingDestructionTextures.Add(pair.Value);
            }

            cachedThumbnails.Clear();
            cachedTextureData.Clear();
            pendingRenderRequests.Clear();
        }
    }

    /// <summary>
    /// Clears a single cached texture
    /// </summary>
    /// <param name="path">The path of the texture</param>
    public static void ClearSingle(string path)
    {
        lock(renderRequestLock)
        {
            if (cachedThumbnails.TryGetValue(path, out var t))
            {
                cachedThumbnails.Remove(path);
                cachedTextureData.Remove(path);

                pendingDestructionTextures.Add(t.texture);
            }

            if (cachedTextures.TryGetValue(path, out var texture))
            {
                cachedTextures.Remove(path);

                pendingDestructionTextures.Add(texture);
            }
        }
    }
}
