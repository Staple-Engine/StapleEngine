using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Staple.Editor;

/// <summary>
/// Loads and generates thumbnails for textures and other assets in the project browser
/// </summary>
internal class ThumbnailCache
{
    private const int ThumbnailSize = 128;

    private class TextureInfo
    {
        public Texture texture;
        public string cachePath;
    }

    private enum RenderRequestType
    {
        Texture,
        Thumbnail,
        Mesh,
        Material,
    }

    private class RenderRequest
    {
        public string path;
        public RenderRequestType type;
        public bool persistentCache;
    }

    private static readonly Dictionary<string, TextureInfo> cachedThumbnails = [];
    private static readonly Dictionary<string, Texture> cachedTextures = [];
    private static readonly List<Texture> pendingDestructionTextures = [];
    private static readonly Dictionary<string, RawTextureData> cachedTextureData = [];
    private static readonly Dictionary<string, Texture> persistentTextures = [];

    private static readonly Dictionary<string, RenderRequest> pendingRenderRequests = [];
    private static readonly Dictionary<string, RenderRequest> pendingMainThreadRenderRequests = [];
    private static readonly Lock renderRequestLock = new();
    private static bool mayRenderInMainThread = true;

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

    private static void MainThreadRenderTask(RenderRequest request)
    {
        void Cleanup()
        {
            pendingMainThreadRenderRequests.Remove(request.path);

            Task.Delay(25).ContinueWith((_) =>
            {
                ThreadHelper.Dispatch(() =>
                {
                    mayRenderInMainThread = true;
                });
            });
        }

        switch(request.type)
        {
            case RenderRequestType.Mesh:
                {
                    var platform = Platform.CurrentPlatform;

                    if (platform.HasValue == false)
                    {
                        Cleanup();

                        break;
                    }

                    var cachePath = EditorUtils.GetLocalPath(request.path);

                    var guid = AssetDatabase.GetAssetGuid(cachePath);

                    if (guid == null)
                    {
                        Cleanup();

                        break;
                    }

                    var mesh = ResourceManager.instance.LoadMeshAsset(cachePath, true);

                    if (mesh == null || mesh.meshes.Count == 0)
                    {
                        Cleanup();

                        break;
                    }

                    var thumbnailPath = Path.Combine(basePath, "Cache", "Thumbnails");

                    try
                    {
                        Directory.CreateDirectory(thumbnailPath);
                    }
                    catch (Exception)
                    {
                    }

                    thumbnailPath = Path.Combine(thumbnailPath, EditorUtils.GetLocalPath(request.path));

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
                        var renderTarget = RenderTarget.Create(ThumbnailSize, ThumbnailSize);

                        if(renderTarget == null)
                        {
                            Cleanup();

                            return;
                        }

                        var tempEntity = Mesh.InstanceMesh("TEMP", mesh);

                        if(tempEntity.IsValid == false)
                        {
                            Cleanup();

                            renderTarget.Destroy();

                            return;
                        }

                        var meshRenderers = tempEntity.GetComponentsInChildren<MeshRenderer>();
                        var skinnedMeshRenderers = tempEntity.GetComponentsInChildren<SkinnedMeshRenderer>();

                        tempEntity.SetLayer((uint)LayerMask.NameToLayer(StapleEditor.RenderTargetLayerName), true);

                        var camera = new Camera()
                        {
                            cameraType = CameraType.Perspective,
                            clearMode = CameraClearMode.SolidColor,
                            clearColor = Color.Clear,
                            nearPlane = 0.001f,
                            farPlane = 1000,
                            fov = 90,
                            cullingLayers = new(LayerMask.GetMask(StapleEditor.RenderTargetLayerName)),
                        };

                        var offset = mesh.Bounds.center + mesh.Bounds.size * 0.75f;
                        var position = new Vector3(-offset.X, offset.Y, offset.Z * 1.5f);
                        var forward = -position.Normalized;

                        var cameraTransform = new Transform
                        {
                            LocalPosition = position,
                            LocalRotation = Math.LookAt(forward, Vector3.Up),
                        };

                        var overrideLights = LightSystem.OverrideLights;
                        var overrideAmbientColor = LightSystem.OverrideAmbientColor;

                        var lightTransform = new Transform();

                        renderTarget.Render(StapleEditor.MeshRenderView, () =>
                        {
                            LightSystem.OverrideLights = [(default, lightTransform, new Light()
                            {
                                type = LightType.Directional,
                                color = Color.White,
                            })];

                            LightSystem.OverrideAmbientColor = Color.Black;

                            RenderSystem.Instance.RenderEntity(default, camera, cameraTransform,
                                tempEntity, tempEntity.GetComponent<Transform>(), false, StapleEditor.MeshRenderView);

                            LightSystem.OverrideLights = overrideLights;
                            LightSystem.OverrideAmbientColor = overrideAmbientColor;

                            RenderSystem.Instance.ClearRenderData(StapleEditor.MeshRenderView);
                        });

                        renderTarget.ReadTexture(StapleEditor.MeshRenderView, 0, (texture, data) =>
                        {
                            tempEntity.Destroy();
                            renderTarget.Destroy();

                            if (texture == null || data == null)
                            {
                                Cleanup();

                                return;
                            }

                            var rawTextureData = new RawTextureData()
                            {
                                colorComponents = StandardTextureColorComponents.RGBA,
                                width = texture.Width,
                                height = texture.Height,
                                data = data,
                            };

                            try
                            {
                                var pngData = rawTextureData.EncodePNG();

                                File.WriteAllBytes(thumbnailPath, pngData);
                            }
                            catch (Exception)
                            {
                            }

                            texture = Texture.CreatePixels(request.path, rawTextureData.data,
                                (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                                new TextureMetadata()
                                {
                                    useMipmaps = false,
                                },
                                TextureFormat.RGBA8);

                            if (texture != null)
                            {
                                lock (renderRequestLock)
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

                        return;
                    }

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

                    var texture = Texture.CreatePixels(request.path, rawTextureData.data,
                        (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                        new TextureMetadata()
                        {
                            useMipmaps = false,
                        },
                        TextureFormat.RGBA8);

                    if (texture != null)
                    {
                        lock (renderRequestLock)
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
                }

                break;

            case RenderRequestType.Material:
                {
                    var platform = Platform.CurrentPlatform;

                    if (platform.HasValue == false)
                    {
                        Cleanup();

                        break;
                    }

                    var cachePath = EditorUtils.GetLocalPath(request.path);

                    var guid = AssetDatabase.GetAssetGuid(cachePath);

                    if (guid == null)
                    {
                        Cleanup();

                        break;
                    }

                    var material = ResourceManager.instance.LoadMaterial(cachePath, true);

                    if (material == null || material.IsValid == false)
                    {
                        Cleanup();

                        break;
                    }

                    var thumbnailPath = Path.Combine(basePath, "Cache", "Thumbnails");

                    try
                    {
                        Directory.CreateDirectory(thumbnailPath);
                    }
                    catch (Exception)
                    {
                    }

                    thumbnailPath = Path.Combine(thumbnailPath, EditorUtils.GetLocalPath(request.path));

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
                        var renderTarget = RenderTarget.Create(ThumbnailSize, ThumbnailSize);

                        if (renderTarget == null)
                        {
                            Cleanup();

                            return;
                        }

                        var tempEntity = Entity.CreatePrimitive(EntityPrimitiveType.Sphere);

                        if (tempEntity.IsValid == false || tempEntity.TryGetComponent<MeshRenderer>(out var meshRenderer) == false)
                        {
                            Cleanup();

                            return;
                        }

                        tempEntity.Name = "TEMP";

                        meshRenderer.materials = Enumerable.Repeat(material, meshRenderer.materials.Count)
                            .ToList();

                        tempEntity.SetLayer((uint)LayerMask.NameToLayer(StapleEditor.RenderTargetLayerName), true);

                        var camera = new Camera()
                        {
                            cameraType = CameraType.Perspective,
                            clearMode = CameraClearMode.SolidColor,
                            clearColor = Color.Clear,
                            nearPlane = 0.001f,
                            farPlane = 1000,
                            fov = 90,
                            cullingLayers = new(LayerMask.GetMask(StapleEditor.RenderTargetLayerName)),
                        };

                        var position = new Vector3(0.5f, 0.5f, -0.5f);

                        var forward = position.Normalized;

                        var cameraTransform = new Transform
                        {
                            LocalPosition = position,
                            LocalRotation = Math.LookAt(forward, Vector3.Up),
                        };

                        meshRenderer.overrideLighting = true;
                        meshRenderer.lighting = MaterialLighting.Lit;

                        var overrideLights = LightSystem.OverrideLights;
                        var overrideAmbientColor = LightSystem.OverrideAmbientColor;

                        var lightTransform = new Transform();

                        renderTarget.Render(StapleEditor.MeshRenderView, () =>
                        {
                            LightSystem.OverrideLights = [(default, lightTransform, new Light()
                            {
                                type = LightType.Directional,
                                color = Color.White,
                            })];

                            LightSystem.OverrideAmbientColor = Color.Black;

                            RenderSystem.Instance.RenderEntity(default, camera, cameraTransform,
                                tempEntity, tempEntity.GetComponent<Transform>(), false, StapleEditor.MeshRenderView);

                            LightSystem.OverrideLights = overrideLights;
                            LightSystem.OverrideAmbientColor = overrideAmbientColor;
                        });

                        renderTarget.ReadTexture(StapleEditor.MeshRenderView, 0, (texture, data) =>
                        {
                            tempEntity.Destroy();
                            renderTarget.Destroy();

                            if (texture == null || data == null)
                            {
                                Cleanup();

                                return;
                            }

                            var rawTextureData = new RawTextureData()
                            {
                                colorComponents = StandardTextureColorComponents.RGBA,
                                width = texture.Width,
                                height = texture.Height,
                                data = data,
                            };

                            try
                            {
                                var pngData = rawTextureData.EncodePNG();

                                File.WriteAllBytes(thumbnailPath, pngData);
                            }
                            catch (Exception)
                            {
                            }

                            texture = Texture.CreatePixels(request.path, rawTextureData.data,
                                (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                                new TextureMetadata()
                                {
                                    useMipmaps = false,
                                },
                                TextureFormat.RGBA8);

                            if (texture != null)
                            {
                                lock (renderRequestLock)
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

                        return;
                    }

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

                    var texture = Texture.CreatePixels(request.path, rawTextureData.data,
                        (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                        new TextureMetadata()
                        {
                            useMipmaps = false,
                        },
                        TextureFormat.RGBA8);

                    if (texture != null)
                    {
                        lock (renderRequestLock)
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
                }

                break;
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
            lock(renderRequestLock)
            {
                pendingRenderRequests.Remove(request.path);
            }
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

                    ThreadHelper.Dispatch(() =>
                    {
                        var texture = Texture.CreatePixels(request.path, rawTextureData.data,
                            (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                            new TextureMetadata()
                            {
                                filter = TextureFilter.Point,
                                format = TextureMetadataFormat.RGBA8,
                                type = TextureType.Texture,
                                useMipmaps = false,
                            },
                            TextureFormat.RGBA8);

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

                    var cachePath = EditorUtils.GetAssetCachePath(basePath, request.path, platform.Value);

                    var thumbnailPath = Path.Combine(basePath, "Cache", "Thumbnails");

                    try
                    {
                        Directory.CreateDirectory(thumbnailPath);
                    }
                    catch (Exception)
                    {
                    }

                    thumbnailPath = Path.Combine(thumbnailPath, EditorUtils.GetLocalPath(request.path));

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
                            rawTextureData.Resize(ThumbnailSize, ThumbnailSize);

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

                    ThreadHelper.Dispatch(() =>
                    {
                        var texture = Texture.CreatePixels(request.path, rawTextureData.data,
                            (ushort)rawTextureData.width, (ushort)rawTextureData.height,
                            new TextureMetadata()
                            {
                                useMipmaps = false,
                            },
                            TextureFormat.RGBA8);

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

    private static void QueueMainThreadRequest(string path, RenderRequest request)
    {
        lock(renderRequestLock)
        {
            if (pendingMainThreadRenderRequests.ContainsKey(path))
            {
                return;
            }

            pendingMainThreadRenderRequests.Add(path, request);
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
            },
            TextureFormat.RGBA8);

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
            (AssetSerialization.TextureExtensions.Contains(extension[1..]) == false &&
            AssetSerialization.MeshExtensions.Contains(extension[1..]) == false &&
            extension != $".{AssetSerialization.MaterialExtension}"))
        {
            return null;
        }

        lock (renderRequestLock)
        {
            if (cachedThumbnails.TryGetValue(path, out var t))
            {
                return t.texture;
            }

            if (pendingRenderRequests.ContainsKey(path) ||
                pendingMainThreadRenderRequests.ContainsKey(path))
            {
                return null;
            }
        }

        var type = RenderRequestType.Thumbnail;

        if(AssetSerialization.MeshExtensions.Contains(extension[1..]))
        {
            type = RenderRequestType.Mesh;

            QueueMainThreadRequest(path, new RenderRequest()
            {
                path = path,
                type = type,
            });

            return null;
        }
        else if(extension == $".{AssetSerialization.MaterialExtension}")
        {
            type = RenderRequestType.Material;

            QueueMainThreadRequest(path, new RenderRequest()
            {
                path = path,
                type = type,
            });

            return null;
        }

        QueueRequest(path, new RenderRequest()
        {
            path = path,
            type = type,
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

        lock(renderRequestLock)
        {
            if(pendingMainThreadRenderRequests.Count > 0 && mayRenderInMainThread)
            {
                var current = pendingMainThreadRenderRequests.FirstOrDefault();

                pendingMainThreadRenderRequests.Remove(current.Key);

                mayRenderInMainThread = false;

                MainThreadRenderTask(current.Value);
            }
        }
    }

    /// <summary>
    /// Removes a pending render request
    /// </summary>
    /// <param name="path">The path of the request</param>
    public static void RemoveRenderRequest(string path)
    {
        lock (renderRequestLock)
        {
            pendingRenderRequests.Remove(path);
            pendingMainThreadRenderRequests.Remove(path);
        }
    }

    /// <summary>
    /// Clears all cached textures (Except persistent cache)
    /// </summary>
    public static void Clear()
    {
        lock(renderRequestLock)
        {
            try
            {
                var thumbnailPath = Path.Combine(basePath, "Cache", "Thumbnails");

                Directory.Delete(thumbnailPath, true);
            }
            catch(Exception)
            {
            }

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
            pendingMainThreadRenderRequests.Clear();
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
