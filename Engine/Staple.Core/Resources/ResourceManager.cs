using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Staple.Internal;

/// <summary>
/// Resource manager. Keeps track of resources.
/// </summary>
internal class ResourceManager
{
    public enum DestroyMode
    {
        Normal,
        Final,
        UserOnly,
    }

    internal static readonly string LogTag = "ResourceManager";

    /// <summary>
    /// Resource paths to load resources from
    /// </summary>
    public List<string> resourcePaths = [];

    internal readonly Dictionary<StringID, Texture> cachedTextures = [];
    internal readonly Dictionary<StringID, Material> cachedMaterials = [];
    internal readonly Dictionary<StringID, Shader> cachedShaders = [];
    internal readonly Dictionary<StringID, ComputeShader> cachedComputeShaders = [];
    internal readonly Dictionary<StringID, Mesh> cachedMeshes = [];
    internal readonly Dictionary<StringID, AudioClip> cachedAudioClips = [];
    internal readonly Dictionary<StringID, MeshAsset> cachedMeshAssets = [];
    internal readonly Dictionary<StringID, FontAsset> cachedFonts = [];
    internal readonly Dictionary<StringID, TextAsset> cachedTextAssets = [];
    internal readonly Dictionary<StringID, IStapleAsset> cachedAssets = [];
    internal readonly Dictionary<StringID, Prefab> cachedPrefabs = [];
    internal readonly Dictionary<StringID, ResourcePak> resourcePaks = [];
    internal readonly List<WeakReference<Texture>> userCreatedTextures = [];
    internal readonly List<WeakReference<VertexBuffer>> userCreatedVertexBuffers = [];
    internal readonly List<WeakReference<IndexBuffer>> userCreatedIndexBuffers = [];
    internal readonly List<WeakReference<Mesh>> userCreatedMeshes = [];

    /// <summary>
    /// Assets that must not be destroyed when using UserOnly mode
    /// </summary>
    internal readonly HashSet<int> lockedAssets = [];

    /// <summary>
    /// Keeps track of all assets that failed to load while a scene was loading them
    /// </summary>
    internal readonly HashSet<StringID> failedSceneAssetLoads = [];

    /// <summary>
    /// Whether we're loading a scene
    /// </summary>
    internal bool loadingScene = false;

    /// <summary>
    /// The default instance of the resource manager
    /// </summary>
    public static ResourceManager instance = new();

    /// <summary>
    /// Reports an asset that failed to load
    /// </summary>
    /// <param name="path">The asset path, if any</param>
    /// <param name="guid">The asset guid, if any</param>
    internal void ReportFailedAssetLoad(string path, string guid)
    {
        if(!loadingScene)
        {
            return;
        }

        if(path != null)
        {
            failedSceneAssetLoads.Add(path);
        }

        if(guid != null)
        {
            failedSceneAssetLoads.Add(guid);
        }
    }

    /// <summary>
    /// Check whether an asset failed to load
    /// </summary>
    /// <param name="path">The asset path, if any</param>
    /// <param name="guid">The asset guid, if any</param>
    /// <returns>Whether the asset failed to load</returns>
    internal bool CheckFailedAssetLoad(string path, string guid)
    {
        if(path != null && failedSceneAssetLoads.Contains(path))
        {
            return true;
        }

        if(guid != null && failedSceneAssetLoads.Contains(guid))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Loads a resource pak to use for resources
    /// </summary>
    /// <param name="path">The path to load from. This does not use the resource paths previously set here</param>
    /// <returns>Whether the pak was loaded</returns>
    internal bool LoadPak(string path)
    {
        try
        {
            if(resourcePaks.ContainsKey(path))
            {
                Platform.ConsoleLog($"Attempted to load resource pak twice for path {path}");

                return false;
            }

            var stream = Platform.platformProvider.OpenFile(path);

            var resourcePak = new ResourcePak();

            if(!resourcePak.Deserialize(stream))
            {
                stream.Dispose();

                Platform.ConsoleLog($"[Error] Failed to load resource pak at {path}: Likely invalid file");

                return false;
            }

            Platform.ConsoleLog($"[Debug] Loaded resource pak at {path}: {resourcePak.FileCount} files");

            resourcePaks.Add(path, resourcePak);

            return true;
        }
        catch(Exception e)
        {
            Platform.ConsoleLog($"[Error] Failed to load resource pak at {path}: {e}");

            return false;
        }
    }

    /// <summary>
    /// Clears all assets that are not locked
    /// </summary>
    internal void Clear()
    {
        Destroy(DestroyMode.UserOnly);

        var destroyed = new HashSet<StringID>();

        foreach (var pair in cachedTextures)
        {
            if(!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMaterials)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedShaders)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedComputeShaders)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMeshes)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedAudioClips)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMeshAssets)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedFonts)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedTextAssets)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedAssets)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedPrefabs)
        {
            if (!lockedAssets.Contains(pair.Key.GetHashCode()))
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach(var key in destroyed)
        {
            cachedTextures.Remove(key);
            cachedMaterials.Remove(key);
            cachedShaders.Remove(key);
            cachedComputeShaders.Remove(key);
            cachedMeshes.Remove(key);
            cachedAudioClips.Remove(key);
            cachedMeshAssets.Remove(key);
            cachedFonts.Remove(key);
            cachedAssets.Remove(key);
            cachedPrefabs.Remove(key);
            cachedTextAssets.Remove(key);
        }
    }

    /// <summary>
    /// Destroys all resources
    /// </summary>
    /// <param name="mode">The kind of destruction to perform</param>
    internal void Destroy(DestroyMode mode)
    {
        if(mode != DestroyMode.UserOnly)
        {
            Material.whiteTexture?.Destroy();
        }

        foreach (var pair in cachedTextures)
        {
            if(mode == DestroyMode.UserOnly && lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
            {
                continue;
            }

            pair.Value?.Destroy();
        }

        foreach (var pair in cachedMaterials)
        {
            if (mode == DestroyMode.UserOnly && lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
            {
                continue;
            }

            pair.Value?.Destroy();
        }

        foreach (var pair in cachedComputeShaders)
        {
            if (mode == DestroyMode.UserOnly && lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
            {
                continue;
            }

            pair.Value?.Destroy();
        }

        foreach (var pair in cachedShaders)
        {
            if (mode == DestroyMode.UserOnly && lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
            {
                continue;
            }

            pair.Value?.Destroy();
        }

        foreach (var pair in cachedMeshes)
        {
            if (mode == DestroyMode.UserOnly && lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
            {
                continue;
            }

            pair.Value?.Destroy();
        }

        if(mode != DestroyMode.UserOnly)
        {
            foreach (var pair in Mesh.defaultMeshes)
            {
                if (lockedAssets.Contains(pair.Value?.Guid.GuidHash ?? 0))
                {
                    continue;
                }

                pair.Value?.Destroy();
            }
        }

        foreach (var item in userCreatedVertexBuffers)
        {
            if(item.TryGetTarget(out var buffer) && !buffer.Disposed)
            {
                buffer.Destroy();
            }
        }

        foreach (var item in userCreatedIndexBuffers)
        {
            if (item.TryGetTarget(out var buffer) && !buffer.Disposed)
            {
                buffer.Destroy();
            }
        }

        userCreatedVertexBuffers.Clear();
        userCreatedIndexBuffers.Clear();

        if (mode == DestroyMode.Final)
        {
            foreach (var pair in resourcePaks)
            {
                pair.Value.Dispose();
            }
        }
    }

    /// <summary>
    /// Attempts to recreate resources (used usually when context is lost and rendering was restarted)
    /// </summary>
    internal void RecreateResources()
    {
        Log.Debug("Recreating resources");

        try
        {
            if(Material.WhiteTexture?.textureResource?.Create() ?? false)
            {
                Log.Debug("Recreated default white texture");
            }
        }
        catch (Exception e)
        {
            Log.Debug($"Failed to recreate default white texture: {e}");
        }

        foreach (var pair in cachedTextures)
        {
            try
            {
                if(pair.Value?.textureResource?.Create() ?? false)
                {
                    Log.Debug($"Recreated texture {pair.Key}");
                }
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate texture: {e}");
            }
        }

        Log.Debug($"Recreating {userCreatedTextures.Count} user-created textures");

        foreach (var reference in userCreatedTextures)
        {
            if (reference.TryGetTarget(out var texture))
            {
                if (!(texture.textureResource?.Create() ?? false))
                {
                    Log.Debug($"Failed to recreate a user texture");
                }
            }
        }

        foreach (var pair in cachedShaders)
        {
            if(pair.Value?.shaderResource == null)
            {
                continue;
            }

            try
            {
                if (pair.Value?.shaderResource?.Create() ?? false)
                {
                    Log.Debug($"Recreated shader {pair.Key}");
                }
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate shader: {e}");
            }
        }

        foreach (var pair in cachedComputeShaders)
        {
            try
            {
                if (pair.Value?.shaderResource?.Create() ?? false)
                {
                    Log.Debug($"Recreated compute shader {pair.Key}");
                }
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate compute shader: {e}");
            }
        }

        foreach (var pair in cachedMaterials)
        {
            if(pair.Value == null)
            {
                continue;
            }

            //TODO
            //pair.Value.Disposed = false;
        }

        foreach (var pair in cachedMeshes)
        {
            if(pair.Value == null)
            {
                continue;
            }

            try
            {
                pair.Value.changed = true;

                pair.Value.UploadMeshData();

                Log.Debug($"Recreated mesh {pair.Key}");
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate mesh: {e}");
            }
        }

        foreach (var pair in Mesh.defaultMeshes)
        {
            if (pair.Value == null)
            {
                continue;
            }

            try
            {
                pair.Value.changed = true;

                pair.Value.UploadMeshData();

                Log.Debug($"Recreated mesh {pair.Key}");
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate mesh: {e}");
            }
        }
    }

    /// <summary>
    /// Attempts to load a file as a byte array
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The byte array, or null</returns>
    public byte[] LoadFile(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var pakPath = path.Replace(Path.DirectorySeparatorChar, '/');

        var guid = AssetDatabase.GetAssetGuid(pakPath);

        if (AssetDatabase.GetAssetPath(path) != null)
        {
            guid = path;
        }

        if(guid != null)
        {
            var localPath = AssetDatabase.GetAssetPath(guid);

            if(localPath != null)
            {
                pakPath = path = localPath;
            }
        }

        foreach(var pair in resourcePaks)
        {
            var pak = pair.Value;

            foreach(var file in pak.Files)
            {
                if(string.Equals(file.path, pakPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    using var stream = pak.Open(file.path);

                    if(stream != null)
                    {
                        var memoryStream = new MemoryStream();

                        stream.CopyTo(memoryStream);

                        return memoryStream.ToArray();
                    }
                }
            }
        }

        if(Path.IsPathRooted(path))
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch(Exception)
            {
                return null;
            }
        }

        foreach(var resourcePath in resourcePaths)
        {
            try
            {
                var p = Path.Combine(resourcePath, path);

                return File.ReadAllBytes(p);
            }
            catch (Exception)
            {
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to load a file as a string
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The string, or null</returns>
    public string LoadFileString(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var data = LoadFile(path);

        if(data == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Attempts to load the scene list
    /// </summary>
    /// <returns>The scene list, or null</returns>
    public List<string> LoadSceneList()
    {
        var data = LoadFile("SceneList");

        if(data == null)
        {
            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SceneListHeader>(data.AsMemory(), out var offset);

            if (header == null || !header.header.SequenceEqual(SceneListHeader.ValidHeader) ||
                header.version != SceneListHeader.ValidVersion)
            {
                return null;
            }

            var sceneData = SerializationUtils.MessagePackDeserialize<SceneList>(data.AsMemory(offset), out _);

            if (sceneData == null || sceneData.scenes == null)
            {
                return null;
            }

            return sceneData.scenes;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to load a raw JSON scene from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The scene, or null</returns>
    public Scene LoadRawSceneFromPath(string path)
    {
        World.Current?.Dispose();

        World.Current = new();

        Scene.current = null;

        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var data = LoadFileString(path);

        if (data == null)
        {
            return null;
        }

        var scene = new Scene();

        loadingScene = true;

        failedSceneAssetLoads.Clear();

        try
        {
            var sceneObjects = JsonSerializer.Deserialize(data, SceneObjectSerializationContext.Default.ListSceneObject);
            var localIDs = new Dictionary<int, Transform>();
            var parents = new Dictionary<int, int>();
            var localSceneObjects = new Dictionary<int, int>();

            for(var i = 0; i < sceneObjects.Count; i++)
            {
                var sceneObject = sceneObjects[i];

                var entity = new Entity();

                switch (sceneObject.kind)
                {
                    case SceneObjectKind.Entity:

                        entity = SceneSerialization.Instantiate(sceneObject, out var localID, false);

                        if (entity == default)
                        {
                            continue;
                        }

                        var transform = entity.GetComponent<Transform>();

                        if(transform == null)
                        {
                            continue;
                        }

                        localIDs.Add(localID, transform);
                        localSceneObjects.Add(localID, i);

                        if(sceneObject.parent >= 0)
                        {
                            parents.Add(localID, sceneObject.parent);
                        }

                        break;
                }
            }

            foreach(var pair in parents)
            {
                if(localIDs.TryGetValue(pair.Key, out var self) && self != null && localIDs.TryGetValue(pair.Value, out var parent))
                {
                    self.SetParent(parent);

                    if(World.Current?.TryGetEntity(self.Entity, out var entityInfo) ?? false)
                    {
                        entityInfo.enabledInHierarchy = parent.Entity.EnabledInHierarchy;
                    }
                }
            }

            foreach(var pair in localIDs)
            {
                var entity = pair.Value.Entity;
                var sceneObject = sceneObjects[localSceneObjects[pair.Key]];

                foreach(var component in sceneObject.components)
                {
                    var componentType = TypeCache.GetType(component.type);

                    if(componentType == null ||
                        !entity.TryGetComponent(componentType, out var componentInstance))
                    {
                        continue;
                    }

                    foreach(var parameter in component.data)
                    {
                        try
                        {
                            var field = componentType.GetField(parameter.Key, BindingFlags.Public | BindingFlags.Instance);

                            if (field == null)
                            {
                                continue;
                            }

                            var element = (JsonElement)parameter.Value;

                            if (field.FieldType == typeof(Entity) && element.ValueKind == JsonValueKind.Number)
                            {
                                var targetEntity = Scene.FindEntity(element.GetInt32());

                                if (targetEntity.IsValid)
                                {
                                    field.SetValue(componentInstance, targetEntity);
                                }
                            }
                            else if ((field.FieldType == typeof(IComponent) ||
                                field.FieldType.GetInterface(typeof(IComponent).FullName) != null) &&
                                element.ValueKind == JsonValueKind.String)
                            {
                                var pieces = element.GetString().Split(":");

                                if (pieces.Length == 2 &&
                                    int.TryParse(pieces[0], out var entityID))
                                {
                                    var targetComponentType = TypeCache.GetType(pieces[1]);

                                    if (targetComponentType == null ||
                                        !targetComponentType.IsAssignableTo(field.FieldType))
                                    {
                                        continue;
                                    }

                                    var targetEntity = Scene.FindEntity(entityID);

                                    if (!targetEntity.IsValid ||
                                        !targetEntity.TryGetComponent(targetComponentType, out var targetComponent))
                                    {
                                        continue;
                                    }

                                    field.SetValue(componentInstance, targetComponent);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }

            loadingScene = false;

            return scene;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load scene {path}: {e}", LogTag);

            loadingScene = false;

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a compiled scene from a guid
    /// </summary>
    /// <param name="guid">The guid to load</param>
    /// <returns>The scene, or null</returns>
    public Scene LoadSceneFromGuid(string guid)
    {
        World.Current?.Dispose();

        World.Current = new();

        Scene.current = null;

        var path = AssetDatabase.GetAssetPath(guid);

        if(path == null)
        {
            World.Current = null;

            World.EmitWorldChangedEvent(true);

            return null;
        }

        var data = LoadFile(guid);

        if (data == null)
        {
            Log.Error($"Failed to load scene at path {path}", LogTag);

            World.Current = null;

            World.EmitWorldChangedEvent(true);

            return null;
        }

        var scene = new Scene();

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableSceneHeader>(data.AsMemory(), out var offset);

            if (header == null || !header.header.SequenceEqual(SerializableSceneHeader.ValidHeader) ||
                header.version != SerializableSceneHeader.ValidVersion)
            {
                Log.Error($"Failed to load scene at path {path}: Invalid header", LogTag);

                World.Current = null;

                World.EmitWorldChangedEvent(true);

                return null;
            }

            var sceneData = SerializationUtils.MessagePackDeserialize<SerializableScene>(data.AsMemory(offset), out _);

            if (sceneData == null || sceneData.objects == null)
            {
                Log.Error($"Failed to load scene at path {path}: Invalid scene data", LogTag);

                World.Current = null;

                World.EmitWorldChangedEvent(true);

                return null;
            }

            var localIDs = new Dictionary<int, Transform>();
            var localSceneObjects = new Dictionary<int, int>();
            var parents = new Dictionary<int, int>();

            loadingScene = true;

            failedSceneAssetLoads.Clear();

            for (var i = 0; i < sceneData.objects.Length; i++)
            {
                ref var sceneObject = ref sceneData.objects[i];

                var entity = new Entity();

                switch (sceneObject.kind)
                {
                    case SceneObjectKind.Entity:

                        entity = SceneSerialization.Instantiate(sceneObject, out var localID, false);

                        if (entity == default)
                        {
                            continue;
                        }

                        var transform = entity.GetComponent<Transform>();

                        localIDs.Add(localID, transform);
                        localSceneObjects.Add(localID, i);

                        if (sceneObject.parent >= 0)
                        {
                            parents.Add(localID, sceneObject.parent);
                        }

                        break;
                }
            }

            foreach (var pair in parents)
            {
                if (localIDs.TryGetValue(pair.Key, out var self) && localIDs.TryGetValue(pair.Value, out var parent))
                {
                    self.SetParent(parent);
                }
            }

            foreach (var pair in localIDs)
            {
                var entity = pair.Value.Entity;
                var sceneObject = sceneData.objects[localSceneObjects[pair.Key]];

                foreach (var component in sceneObject.components)
                {
                    var componentType = TypeCache.GetType(component.type);

                    if (componentType == null ||
                        !entity.TryGetComponent(componentType, out var componentInstance))
                    {
                        continue;
                    }

                    foreach (var parameter in component.parameters)
                    {
                        try
                        {
                            var field = componentType.GetField(parameter.Key, BindingFlags.Public | BindingFlags.Instance);

                            if (field == null)
                            {
                                continue;
                            }

                            if (field.FieldType == typeof(Entity) && parameter.Value is int intValue)
                            {
                                var targetEntity = Scene.FindEntity(intValue);

                                if (targetEntity.IsValid)
                                {
                                    field.SetValue(componentInstance, targetEntity);
                                }
                            }
                            else if ((field.FieldType == typeof(IComponent) ||
                                field.FieldType.GetInterface(typeof(IComponent).FullName) != null) &&
                                parameter.Value is string stringValue)
                            {
                                var pieces = stringValue.Split(":");

                                if (pieces.Length == 2 &&
                                    int.TryParse(pieces[0], out var entityID))
                                {
                                    var targetComponentType = TypeCache.GetType(pieces[1]);

                                    if (targetComponentType == null ||
                                        !targetComponentType.IsAssignableTo(field.FieldType))
                                    {
                                        continue;
                                    }

                                    var targetEntity = Scene.FindEntity(entityID);

                                    if (!targetEntity.IsValid ||
                                        !targetEntity.TryGetComponent(targetComponentType, out var targetComponent))
                                    {
                                        continue;
                                    }

                                    field.SetValue(componentInstance, targetComponent);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }

            foreach(var pair in localIDs)
            {
                var entity = pair.Value.Entity;

                entity.IterateComponents((ref c) =>
                {
                    if (Platform.IsPlaying && c is CallbackComponent callback)
                    {
                        try
                        {
                            callback.Awake();
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"{entity.Name} ({callback.GetType().FullName}): Exception thrown while handling Awake: {e}");
                        }
                    }

                    World.Current?.EmitAddComponentEvent(entity, ref c);
                });
            }

            World.EmitWorldChangedEvent(true);

            loadingScene = false;

            return scene;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load scene at path {path}: {e}", LogTag);

            World.Current = null;

            World.EmitWorldChangedEvent(true);

            loadingScene = false;

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a scene from a scene name
    /// </summary>
    /// <param name="name">The scene name</param>
    /// <returns>The scene, or null</returns>
    public Scene LoadScene(string name)
    {
        if ((name?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(name) ??
            name;

        var assetType = AssetDatabase.GetAssetType(guid);

        if(assetType != typeof(Scene).FullName)
        {
            if(assetType == null)
            {
                Log.Debug($"Failed to load scene {name}: Failed to find asset", LogTag);
            }
            else
            {
                Log.Debug($"Failed to load scene {name}: Invalid asset type {assetType}", LogTag);
            }

            return null;
        }

        return LoadSceneFromGuid(guid);
    }

    /// <summary>
    /// Attempts to load shader data from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The shader data, or null</returns>
    public SerializableShader LoadShaderData(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            Log.Error($"Failed to load shader data: invalid path", LogTag);

            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        var assetPath = AssetDatabase.GetAssetPath(path);

        if (assetPath != null)
        {
            guid = path;
        }
        else
        {
            guid = AssetDatabase.GetAssetGuid(path) ?? guid;
        }

        if (guid == null)
        {
            Log.Error($"Failed to load shader data: invalid guid", LogTag);

            return null;
        }

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(guid);

        if (data == null)
        {
            Log.Error($"Failed to load shader at guid {guid}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableShaderHeader>(data.AsMemory(), out var offset);

            if (header == null || !header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) ||
                header.version != SerializableShaderHeader.ValidVersion)
            {
                Log.Error($"Failed to load shader at guid {guid}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var shaderData = SerializationUtils.MessagePackDeserialize<SerializableShader>(data.AsMemory(offset), out _);

            if (shaderData == null || shaderData.metadata == null)
            {
                Log.Error($"Failed to load shader at guid {guid}: Invalid shader data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            return shaderData;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load shader at guid {guid}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a shader resource from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The shader resource, or null</returns>
    public ShaderResource LoadShaderResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        var assetPath = AssetDatabase.GetAssetPath(path);

        if (assetPath != null)
        {
            guid = path;
            path = assetPath;
        }
        else
        {
            guid = AssetDatabase.GetAssetGuid(path) ?? guid;
        }

        if (guid == null || CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(guid);

        data ??= LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load shader at path {path}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableShaderHeader>(data.AsMemory(), out var offset);

            if (header == null || !header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) ||
                header.version != SerializableShaderHeader.ValidVersion)
            {
                Log.Error($"Failed to load shader at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var shaderData = SerializationUtils.MessagePackDeserialize<SerializableShader>(data.AsMemory(offset), out _);

            if (shaderData == null || shaderData.metadata == null)
            {
                Log.Error($"Failed to load shader at path {path}: Invalid shader data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            if (shaderData.metadata.type != ShaderType.VertexFragment)
            {
                Log.Error($"Failed to load shader at path {path}: Not a vertex/fragment shader", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            if (!shaderData.data.TryGetValue(RenderWindow.CurrentRenderer, out var entries))
            {
                Log.Error($"Failed to load shader at path {path}: Missing shader data for renderer {RenderWindow.CurrentRenderer}",
                    LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            foreach (var pair in entries.data)
            {
                if ((pair.Value.vertexShader?.Length ?? 0) == 0 || (pair.Value.fragmentShader?.Length ?? 0) == 0)
                {
                    ReportFailedAssetLoad(path, guid);

                    return null;
                }
            }

            var resource = Shader.Create(shaderData, entries.data);

            if (resource != null)
            {
                resource.Guid.Guid = guid;

                return resource;
            }

            Log.Error($"Failed to load shader at path {path}: Failed to initialize shader", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load shader at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a shader from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The shader, or null</returns>
    public Shader LoadShader(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        var assetPath = AssetDatabase.GetAssetPath(path);

        if(assetPath != null)
        {
            guid = path;
            path = assetPath;
        }
        else
        {
            guid = AssetDatabase.GetAssetGuid(path) ?? guid;
        }

        if (guid == null)
        {
            return null;
        }

        if (!ignoreCache &&
            cachedShaders.TryGetValue(path, out var shader) &&
            shader != null &&
            !shader.Disposed)
        {
            return shader;
        }

        var resource = LoadShaderResource(path);

        if (resource == null)
        {
            ReportFailedAssetLoad(path, guid);

            return null;
        }

        shader = new(resource);

        if (!ignoreCache)
        {
            cachedShaders.AddOrSetKey(path, shader);
        }

        return shader;
    }

    /// <summary>
    /// Attempts to load a compute shader resource from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The shader resource, or null</returns>
    public ComputeShaderResource LoadComputeShaderResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        guid = AssetDatabase.GetAssetGuid(path) ?? guid;

        if (guid == null || CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(guid);

        data ??= LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load compute shader at path {path}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableShaderHeader>(data.AsMemory(), out var offset);

            if (header == null || !header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) ||
                header.version != SerializableShaderHeader.ValidVersion)
            {
                Log.Error($"Failed to load compute shader at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var shaderData = SerializationUtils.MessagePackDeserialize<SerializableShader>(data.AsMemory(offset), out _);

            if (shaderData == null || shaderData.metadata == null)
            {
                Log.Error($"Failed to load compute shader at path {path}: Invalid shader data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            if (shaderData.metadata.type != ShaderType.Compute)
            {
                Log.Error($"Failed to load compute shader at path {path}: Not a compute shader", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            if (!shaderData.data.TryGetValue(RenderWindow.CurrentRenderer, out var entries))
            {
                Log.Error($"Failed to load compute shader at path {path}: Missing shader data for renderer {RenderWindow.CurrentRenderer}",
                    LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            foreach (var pair in entries.data)
            {
                if ((pair.Value.computeShader?.Length ?? 0) == 0)
                {
                    ReportFailedAssetLoad(path, guid);

                    return null;
                }
            }

            var shader = ComputeShader.Create(shaderData, entries.data);

            if (shader != null)
            {
                shader.Guid.Guid = guid;

                return shader;
            }

            Log.Error($"Failed to load compute shader at path {path}: Failed to initialize shader", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load compute shader at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a compute shader from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The shader, or null</returns>
    public ComputeShader LoadComputeShader(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        guid = AssetDatabase.GetAssetGuid(path) ?? guid;

        if (guid == null || CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        if (!ignoreCache &&
            cachedComputeShaders.TryGetValue(path, out var shader) &&
            shader != null &&
            !shader.Disposed)
        {
            return shader;
        }

        var resource = LoadComputeShaderResource(path);

        if (resource == null)
        {
            return null;
        }

        shader = new(resource);

        if (!ignoreCache)
        {
            cachedComputeShaders.AddOrSetKey(path, shader);
        }

        return shader;
    }

    /// <summary>
    /// Attempts to load a material resource from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The material resource, or null</returns>
    public MaterialResource LoadMaterialResource(string path)
    {
        if ((path?.Length ?? 0) == 0 || CheckFailedAssetLoad(path, path))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load material at path {path}", LogTag);

            ReportFailedAssetLoad(path, path);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableMaterialHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableMaterialHeader.ValidHeader) ||
                header.version != SerializableMaterialHeader.ValidVersion)
            {
                Log.Error($"Failed to load material at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, path);

                return null;
            }

            var materialData = SerializationUtils.MessagePackDeserialize<SerializableMaterial>(data.AsMemory(offset), out _);

            if (materialData == null || materialData.metadata == null)
            {
                Log.Error($"Failed to load material at path {path}: Invalid data", LogTag);

                ReportFailedAssetLoad(path, path);

                return null;
            }

            if ((materialData.metadata.shader?.Length ?? 0) == 0)
            {
                Log.Error($"Failed to load material at path {path}: Invalid shader path", LogTag);

                ReportFailedAssetLoad(path, path);

                return null;
            }

            var shader = LoadShader(materialData.metadata.shader, false);

            if (shader == null)
            {
                Log.Error($"Failed to load material at path {path}: Failed to load shader", LogTag);

                ReportFailedAssetLoad(path, path);

                return null;
            }

            var guid = AssetDatabase.GetAssetGuid(path);

            var materialResource = new MaterialResource()
            {
                metadata = materialData.metadata,
                shader = shader,
            };

            materialResource.Guid.Guid = guid ?? path;

            return materialResource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load material at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, path);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a material from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The material, or null</returns>
    public Material LoadMaterial(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        if (!ignoreCache &&
            cachedMaterials.TryGetValue(path, out var material) &&
            material != null &&
            !material.Disposed)
        {
            return material;
        }

        var resource = LoadMaterialResource(path);

        if (resource == null)
        {
            return null;
        }

        material = new Material
        {
            materialResource = resource,
            CullingMode = resource.metadata.cullingMode,
        };

        foreach (var variant in resource.metadata.enabledShaderVariants)
        {
            if (resource.shader.shaderResource.metadata.variants.Contains(variant))
            {
                material.EnableShaderKeyword(variant);
            }
        }

        foreach (var parameter in resource.metadata.parameters)
        {
            switch (parameter.Value.type)
            {
                case MaterialParameterType.TextureWrap:

                    material.materialResource.parameters.Add(parameter.Key, new()
                    {
                        name = parameter.Key,
                        type = MaterialParameterType.TextureWrap,
                        textureWrapValue = parameter.Value.textureWrapValue,
                    });

                    break;

                case MaterialParameterType.Texture:

                    var texture = (parameter.Value.textureValue?.Length ?? 0) > 0 ? LoadTexture(parameter.Value.textureValue) : null;

                    material.SetTexture(parameter.Key, texture);

                    break;

                case MaterialParameterType.Matrix3x3:

                    material.SetMatrix3x3(parameter.Key, new(), parameter.Value.source);

                    break;

                case MaterialParameterType.Matrix4x4:

                    material.SetMatrix4x4(parameter.Key, new(), parameter.Value.source);

                    break;

                case MaterialParameterType.Vector2:

                    material.SetVector2(parameter.Key, parameter.Value.vec2Value.ToVector2(), parameter.Value.source);

                    break;

                case MaterialParameterType.Vector3:

                    material.SetVector3(parameter.Key, parameter.Value.vec3Value.ToVector3(), parameter.Value.source);

                    break;

                case MaterialParameterType.Vector4:

                    material.SetVector4(parameter.Key, parameter.Value.vec4Value.ToVector4(), parameter.Value.source);

                    break;

                case MaterialParameterType.Color:

                    material.SetColor(parameter.Key, parameter.Value.colorValue, parameter.Value.source);

                    break;

                case MaterialParameterType.Float:

                    material.SetFloat(parameter.Key, parameter.Value.floatValue, parameter.Value.source);

                    break;

                case MaterialParameterType.Int:

                    material.SetInt(parameter.Key, parameter.Value.intValue, parameter.Value.source);

                    break;
            }
        }

        if (!ignoreCache)
        {
            cachedMaterials.AddOrSetKey(path, material);
        }

        return material;
    }

    public TextureResource LoadTextureResource(string path, TextureFlags flags = TextureFlags.None)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load texture at path {path}: File not found", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableTextureHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) ||
                header.version != SerializableTextureHeader.ValidVersion)
            {
                Log.Error($"Failed to load texture at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var textureData = SerializationUtils.MessagePackDeserialize<SerializableTexture>(data.AsMemory(offset), out _);

            if (textureData == null)
            {
                Log.Error($"Failed to load texture at path {path}: Invalid texture data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var resource = Texture.Create(path, textureData, flags);

            if (resource == null)
            {
                Log.Error($"Failed to load texture at path {path}: Failed to create texture", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            if (textureData.cpuData != null)
            {
                resource.readbackData = new()
                {
                    colorComponents = textureData.cpuData.colorComponents,
                    data = textureData.cpuData.data,
                    width = textureData.cpuData.width,
                    height = textureData.cpuData.height,
                };
            }

            return resource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load texture at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a texture from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="flags">Any additional texture flags</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The texture, or null</returns>
    public Texture LoadTexture(string path, TextureFlags flags = TextureFlags.None, bool ignoreCache = false)
    {
        if((path?.Length ?? 0) == 0)
        {
            return null;
        }

        if(path == "WHITE")
        {
            return Material.WhiteTexture;
        }

        if(!ignoreCache &&
            cachedTextures.TryGetValue(path, out var texture) &&
            texture != null &&
            !texture.Disposed)
        {
            return texture;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        var resource = LoadTextureResource(path, flags);

        if(resource == null)
        {
            return null;
        }

        texture = new Texture(resource);

        texture.ApplyTextureToSprites();

        if (!ignoreCache)
        {
            cachedTextures.AddOrSetKey(path, texture);
        }

        return texture;
    }

    /// <summary>
    /// Attempts to load an audio clip resource from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The audio clip resource, or null</returns>
    public AudioClipResource LoadAudioClipResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load audio clip at path {path}: File not found", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableAudioClipHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableAudioClipHeader.ValidHeader) ||
                header.version != SerializableAudioClipHeader.ValidVersion)
            {
                Log.Error($"Failed to load audio clip at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var audioData = SerializationUtils.MessagePackDeserialize<SerializableAudioClip>(data.AsMemory(offset), out _);

            if (audioData == null)
            {
                Log.Error($"Failed to load audio clip at path {path}: Invalid audio data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var resource = new AudioClipResource()
            {
                metadata = audioData.metadata,
                fileData = audioData.fileData,
                format = audioData.format,
            };

            resource.Guid.Guid = path;

            return resource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load audio clip at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load an audio clip from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The audio clip, or null</returns>
    public AudioClip LoadAudioClip(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        if (!ignoreCache &&
            cachedAudioClips.TryGetValue(path, out var audioClip) &&
            audioClip != null)
        {
            return audioClip;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        var resource = LoadAudioClipResource(path);

        if (resource == null)
        {
            return null;
        }

        audioClip = new AudioClip()
        {
            audioResource = resource,
        };

        if (!ignoreCache)
        {
            cachedAudioClips.AddOrSetKey(path, audioClip);
        }

        return audioClip;
    }

    /// <summary>
    /// Attempts to load a mesh from a path
    /// </summary>
    /// <param name="guid">The guid to the mesh file. This guid can have a special terminator to indicate the mesh index (guid:index)</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The mesh, or null</returns>
    public Mesh LoadMesh(string guid, bool ignoreCache = false)
    {
        if ((guid?.Length ?? 0) == 0 || CheckFailedAssetLoad(guid, guid))
        {
            return null;
        }

        if (guid.StartsWith("Internal/", StringComparison.InvariantCulture))
        {
            return Mesh.GetDefaultMesh(guid);
        }

        var original = guid;

        var indexString = "0";

        if(guid.Contains(':'))
        {
            var split = guid.Split(':');

            if(split.Length != 2)
            {
                return null;
            }

            guid = split[0];
            indexString = split[1];
        }

        if (!ignoreCache &&
            cachedMeshes.TryGetValue(original, out var mesh) &&
            mesh != null)
        {
            return mesh;
        }

        var asset = LoadMeshAsset(guid);

        if (asset == null || asset.Meshes.Length == 0)
        {
            if(asset != null && asset.Meshes.Length == 0)
            {
                Log.Error($"Failed to load mesh {original}: Asset contains no mesh data", LogTag);

                ReportFailedAssetLoad(original, original);
            }

            return null;
        }

        var meshIndex = 0;

        if(!string.IsNullOrEmpty(indexString))
        {
            if(!int.TryParse(indexString, out meshIndex))
            {
                meshIndex = Array.FindIndex(asset.Meshes, x => x.name == indexString);
            }
        }

        if(meshIndex < 0 || meshIndex >= asset.Meshes.Length)
        {
            Log.Error($"Failed to load mesh {original}: Invalid mesh index {meshIndex}", LogTag);

            ReportFailedAssetLoad(original, original);

            return null;
        }

        var m = asset.Meshes[meshIndex];

        mesh = new Mesh(true, false)
        {
            meshTopology = m.topology,
            indexFormat = MeshIndexFormat.UInt32,
            bounds = m.transformedBounds,

            meshAsset = asset,
            meshAssetIndex = meshIndex,
        };

        foreach (var submesh in m.submeshes)
        {
            mesh.AddSubmesh(submesh.startVertex, submesh.vertexCount, submesh.startIndex, submesh.indexCount, m.topology);
        }

        mesh.changed = true;

        if(!mesh.HasBoneIndices)
        {
            mesh.MarkStaticMesh();
        }

        mesh.Guid.Guid = (original.Contains('/') || original.Contains('\\')) ? $"{asset.Guid}:{meshIndex}" : original;

        if(!ignoreCache)
        {
            cachedMeshes.AddOrSetKey(original, mesh);
        }

        return mesh;
    }

    /// <summary>
    /// Attempts to load a mesh asset resource from a path
    /// </summary>
    /// <param name="path">The path to the mesh asset file</param>
    /// <returns>The mesh asset resource, or null</returns>
    public MeshAssetResource LoadMeshAssetResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"Failed to load mesh asset at path {path}: File not found", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableMeshAssetHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableMeshAssetHeader.ValidHeader) ||
                header.version != SerializableMeshAssetHeader.ValidVersion)
            {
                Log.Error($"Failed to load mesh asset at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var meshAssetData = SerializationUtils.MessagePackDeserialize<SerializableMeshAsset>(data.AsMemory(offset), out _);

            if (meshAssetData == null)
            {
                Log.Error($"Failed to load mesh asset at path {path}: Invalid mesh asset data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return null;
            }

            var resource = new MeshAssetResource()
            {
                lighting = meshAssetData.metadata.lighting,
                frameRate = meshAssetData.metadata.frameRate,
                syncAnimationToRefreshRate = meshAssetData.metadata.syncAnimationToRefreshRate,
            };

            resource.Guid.Guid = guid ?? path;

            resource.nodes = new MeshAsset.Node[meshAssetData.nodes.Length];

            for (var i = 0; i < meshAssetData.nodes.Length; i++)
            {
                var node = meshAssetData.nodes[i];

                var transform = Matrix4x4.TRS(node.position.ToVector3(), node.scale.ToVector3(), node.rotation.ToQuaternion());

                resource.nodes[i] = new MeshAsset.Node()
                {
                    name = node.name,
                    index = i,
                    Transform = transform,
                    OriginalTransform = transform,
                    meshIndices = [.. node.meshIndices],
                    children = [.. node.children],
                };
            }

            for (var i = 0; i < meshAssetData.nodes.Length; i++)
            {
                var node = meshAssetData.nodes[i];

                resource.nodes[i].parent = resource.nodes.FirstOrDefault(x => x.children.Contains(i));
            }

            var startBoneIndex = 0;

            resource.meshes = new MeshAsset.MeshInfo[meshAssetData.meshes.Length];

            for(var i = 0; i < resource.meshes.Length; i++)
            {
                var m = meshAssetData.meshes[i];

                var newMesh = new MeshAsset.MeshInfo()
                {
                    name = m.name,
                    topology = m.topology,
                    lighting = resource.lighting,
                    type = m.type,
                    components = m.Components,

                    bounds = new AABB(m.boundsCenter.ToVector3(), m.boundsExtents.ToVector3()),

                    vertices = [.. m.vertices.Select(x => x.ToVector3())],

                    normals = [.. m.normals.Select(x => x.ToVector3())],

                    colors = [.. m.colors
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        })],

                    colors2 = [.. m.colors2
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        })],

                    colors3 = [.. m.colors3
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        })],

                    colors4 = [.. m.colors4
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        })],

                    tangents = [.. m.tangents.Select(x => x.ToVector3())],

                    bitangents = [.. m.bitangents.Select(x => x.ToVector3())],

                    UV1 = [.. m.UV1.Select(x => x.ToVector2())],

                    UV2 = [.. m.UV2.Select(x => x.ToVector2())],

                    UV3 = [.. m.UV3.Select(x => x.ToVector2())],

                    UV4 = [.. m.UV4.Select(x => x.ToVector2())],

                    UV5 = [.. m.UV5.Select(x => x.ToVector2())],

                    UV6 = [.. m.UV6.Select(x => x.ToVector2())],

                    UV7 = [.. m.UV7.Select(x => x.ToVector2())],

                    UV8 = [.. m.UV8.Select(x => x.ToVector2())],

                    indices = [.. m.indices],

                    boneIndices = [.. m.boneIndices.Select(x => x.ToVector4())],

                    boneWeights = [.. m.boneWeights.Select(x => x.ToVector4())],

                    startBoneIndex = startBoneIndex,

                    bones = [.. m.bones.Select(x => new MeshAsset.Bone()
                    {
                        nodeIndex = x.nodeIndex,
                        offsetMatrix = x.offsetMatrix.ToMatrix(),
                    })],
                };

                for (var j = 0; j < newMesh.boneIndices.Length; j++)
                {
                    var index = newMesh.boneIndices[j];

                    if (index.X >= 0)
                    {
                        index.X += startBoneIndex;
                    }

                    if (index.Y >= 0)
                    {
                        index.Y += startBoneIndex;
                    }

                    if (index.Z >= 0)
                    {
                        index.Z += startBoneIndex;
                    }

                    if (index.W >= 0)
                    {
                        index.W += startBoneIndex;
                    }

                    newMesh.boneIndices[j] = index;
                }

                startBoneIndex += newMesh.bones.Length;

                newMesh.submeshes = [new()
                {
                    startVertex = 0,
                    startIndex = 0,
                    vertexCount = m.vertices.Length,
                    indexCount = m.indices.Length,
                }];

                newMesh.submeshMaterialGuids = [m.materialGuid];

                newMesh.transformedBounds = newMesh.bounds;

                if (newMesh.type == MeshAssetType.Skinned)
                {
                    foreach (var node in resource.nodes)
                    {
                        if (node.meshIndices.Contains(resource.meshes.Length))
                        {
                            Matrix4x4.Decompose(node.OriginalGlobalTransform, out var scale, out var rotation, out var position);

                            var size = Vector3.Abs((newMesh.bounds.size * scale).Transformed(rotation));

                            var center = (newMesh.bounds.center * scale).Transformed(rotation);

                            newMesh.transformedBounds = new(center + position, size);

                            break;
                        }
                    }
                }

                resource.meshes[i] = newMesh;
            }

            resource.BoneCount = startBoneIndex;

            if (resource.meshes.Length == 1)
            {
                resource.Bounds = resource.meshes[0].transformedBounds;
            }
            else if (resource.meshes.Length > 0)
            {
                var min = Vector3.One * 999999;
                var max = Vector3.One * -999999;

                foreach (var m in resource.meshes)
                {
                    if (min.X > m.transformedBounds.center.X)
                    {
                        min.X = m.transformedBounds.center.X;
                    }

                    if (min.Y > m.transformedBounds.center.Y)
                    {
                        min.Y = m.transformedBounds.center.Y;
                    }

                    if (min.Z > m.transformedBounds.center.Z)
                    {
                        min.Z = m.transformedBounds.center.Z;
                    }

                    if (max.X < m.transformedBounds.center.X + m.transformedBounds.size.X)
                    {
                        max.X = m.transformedBounds.center.X + m.transformedBounds.size.X;
                    }

                    if (max.Y < m.transformedBounds.center.Y + m.transformedBounds.size.Y)
                    {
                        max.Y = m.transformedBounds.center.Y + m.transformedBounds.size.Y;
                    }

                    if (max.Z < m.transformedBounds.center.Z + m.transformedBounds.size.Z)
                    {
                        max.Z = m.transformedBounds.center.Z + m.transformedBounds.size.Z;
                    }
                }

                resource.Bounds = AABB.CreateFromMinMax(min, max);
            }

            foreach (var a in meshAssetData.animations)
            {
                var animation = new MeshAsset.Animation()
                {
                    name = a.name,
                    duration = a.duration,
                };

                foreach (var c in a.channels)
                {
                    var channel = new MeshAsset.AnimationChannel()
                    {
                        nodeIndex = c.nodeIndex,
                        positions = [.. c.positionKeys.Select(x => new MeshAsset.AnimationKey<Vector3>()
                        {
                            time = x.time,
                            value = x.value.ToVector3(),
                        })],

                        rotations = [.. c.rotationKeys.Select(x => new MeshAsset.AnimationKey<Quaternion>()
                        {
                            time = x.time,
                            value = new Quaternion(x.value.x, x.value.y, x.value.z, x.value.w),
                        })],

                        scales = [.. c.scaleKeys.Select(x => new MeshAsset.AnimationKey<Vector3>()
                        {
                            time = x.time,
                            value = x.value.ToVector3(),
                        })],
                    };

                    animation.channels.Add(channel);
                }

                resource.animations.AddOrSetKey(animation.name, animation);
            }

            return resource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load mesh asset at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a mesh asset from a path
    /// </summary>
    /// <param name="path">The path to the mesh asset file</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The mesh asset, or null</returns>
    public MeshAsset LoadMeshAsset(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if (!ignoreCache &&
            cachedMeshAssets.TryGetValue(path, out var mesh) &&
            mesh != null)
        {
            return mesh;
        }

        var resource = LoadMeshAssetResource(path);

        if(resource == null)
        {
            return null;
        }

        var asset = new MeshAsset()
        {
            meshResource = resource,
        };

        if (!ignoreCache)
        {
            cachedMeshAssets.AddOrSetKey(path, asset);
        }

        return asset;
    }

    /// <summary>
    /// Attempts to load an asset of a specific type from a path
    /// </summary>
    /// <typeparam name="T">The asset type, which must implement IStapleAsset</typeparam>
    /// <param name="path">The path to load from</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The asset, or null</returns>
    public T LoadAsset<T>(string path, bool ignoreCache = false) where T: IStapleAsset
    {
        if ((path?.Length ?? 0) == 0)
        {
            return default;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        object value = LoadAsset(path, ignoreCache);

        if(value == null)
        {
            return default;
        }

        if (value.GetType() != typeof(T))
        {
            Log.Error($"Failed to load asset at path {path}: Type {value.GetType().FullName} is not matching requested type {typeof(T).FullName}",
                LogTag);

            return default;
        }

        return (T)value;
    }

    /// <summary>
    /// Attempts to load an asset of a specific type from a path
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The asset, or null</returns>
    public IStapleAsset LoadAsset(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return default;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return default;
        }

        if (!ignoreCache &&
            cachedAssets.TryGetValue(path, out var asset) &&
            asset != null)
        {
            return asset;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            ReportFailedAssetLoad(path, guid);

            return default;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableStapleAssetHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) ||
                header.version != SerializableStapleAssetHeader.ValidVersion)
            {
                Log.Error($"Failed to load asset at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var assetBundle = SerializationUtils.MessagePackDeserialize<SerializableStapleAsset>(data.AsMemory(offset), out _);

            if (assetBundle == null)
            {
                Log.Error($"Failed to load asset at path {path}: Invalid asset data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            asset = AssetSerialization.Deserialize(assetBundle, StapleSerializationMode.Binary);

            if (asset != null)
            {
                if(asset is IGuidAsset guidAsset)
                {
                    guidAsset.Guid.Guid = path;
                }

                if(!ignoreCache)
                {
                    cachedAssets.AddOrSetKey(path, asset);
                }
            }

            return asset;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load asset at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return default;
        }
    }

    /// <summary>
    /// Attempts to load a raw JSON prefab from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <returns>The prefab, or null</returns>
    public Prefab LoadRawPrefabFromPath(string path)
    {
        var data = LoadFileString(path);

        if (data == null || CheckFailedAssetLoad(path, path))
        {
            return null;
        }

        try
        {
            var prefab = JsonSerializer.Deserialize(data, SerializablePrefabSerializationContext.Default.SerializablePrefab);

            var outValue = new Prefab()
            {
                data = prefab,
            };

            outValue.Guid.Guid = prefab.guid;

            return outValue;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load prefab at {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, path);

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a prefab
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <param name="ignoreCache">Whether to ignore the asset cache</param>
    /// <returns>The prefab, or null</returns>
    public Prefab LoadPrefab(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        if (!ignoreCache &&
            cachedPrefabs.TryGetValue(path, out var prefab) &&
            prefab != null)
        {
            return prefab;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            ReportFailedAssetLoad(path, guid);

            return default;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializablePrefabHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializablePrefabHeader.ValidHeader) ||
                header.version != SerializablePrefabHeader.ValidVersion)
            {
                Log.Error($"Failed to load prefab at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var prefabData = SerializationUtils.MessagePackDeserialize<SerializablePrefab>(data.AsMemory(offset), out _);

            if (prefabData == null)
            {
                Log.Error($"Failed to load prefab at path {path}: Invalid prefab data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            prefab = new()
            {
                data = prefabData,
            };

            prefab.Guid.Guid = guid ?? path;

            if (!ignoreCache)
            {
                cachedPrefabs.AddOrSetKey(path, prefab);
            }

            return prefab;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load prefab at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return default;
        }
    }

    /// <summary>
    /// Attempts to load a font resource
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <returns>The font resource, or null</returns>
    public FontAssetResource LoadFontResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            ReportFailedAssetLoad(path, guid);

            return default;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableFontHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableFontHeader.ValidHeader) ||
                header.version != SerializableFontHeader.ValidVersion)
            {
                Log.Error($"Failed to load font at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var fontData = SerializationUtils.MessagePackDeserialize<SerializableFont>(data.AsMemory(offset), out _);

            if (fontData == null)
            {
                Log.Error($"Failed to load font at path {path}: Invalid font data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var resource = new FontAssetResource()
            {
                metadata = fontData.metadata,
            };

            resource.Guid.Guid = path;

            resource.font = TextFont.FromData(fontData.fontData, resource.Guid.Guid, fontData.metadata.useAntiAliasing,
                fontData.metadata.textureSize, fontData.metadata.includedCharacterSets);

            return resource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load font at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return default;
        }
    }

    /// <summary>
    /// Attempts to load a font
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <param name="ignoreCache">Whether to ignore the asset cache</param>
    /// <returns>The font, or null</returns>
    public FontAsset LoadFont(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if (!ignoreCache &&
            cachedFonts.TryGetValue(path, out var font) &&
            font != null)
        {
            return font;
        }

        var resource = LoadFontResource(path);

        if(resource == null)
        {
            return null;
        }

        font = new()
        {
            fontResource = resource,
        };

        if (!ignoreCache)
        {
            cachedFonts.AddOrSetKey(path, font);
        }

        return font;
    }

    /// <summary>
    /// Attempts to load a text or binary file as a text asset resource
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <returns>The text asset resource, or null</returns>
    public TextAssetResource LoadTextAssetResource(string path)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if(CheckFailedAssetLoad(path, guid))
        {
            return null;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            ReportFailedAssetLoad(path, guid);

            return default;
        }

        try
        {
            var header = SerializationUtils.MessagePackDeserialize<SerializableTextAssetHeader>(data.AsMemory(), out var offset);

            if (header == null ||
                !header.header.SequenceEqual(SerializableTextAssetHeader.ValidHeader) ||
                header.version != SerializableTextAssetHeader.ValidVersion)
            {
                Log.Error($"Failed to load text asset at path {path}: Invalid header", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var textData = SerializationUtils.MessagePackDeserialize<SerializableTextAsset>(data.AsMemory(offset), out _);

            if (textData == null)
            {
                Log.Error($"Failed to load text asset at path {path}: Invalid data", LogTag);

                ReportFailedAssetLoad(path, guid);

                return default;
            }

            var resource = new TextAssetResource()
            {
                bytes = textData.data,
            };

            resource.Guid.Guid = textData.metadata.guid;

            var assetPath = AssetDatabase.GetAssetPath(path);

            var extension = Path.GetExtension(assetPath ?? path);

            if (!string.IsNullOrEmpty(extension) &&
                Array.IndexOf(AssetSerialization.TextExtensions, extension[1..]) >= 0)
            {
                try
                {
                    resource.text = Encoding.UTF8.GetString(resource.bytes);
                }
                catch (Exception)
                {
                }
            }

            return resource;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load text asset at path {path}: {e}", LogTag);

            ReportFailedAssetLoad(path, guid);

            return default;
        }
    }

    /// <summary>
    /// Attempts to load a text or binary file as a text asset
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <param name="ignoreCache">Whether to ignore the asset cache</param>
    /// <returns>The text asset, or null</returns>
    public TextAsset LoadTextAsset(string path, bool ignoreCache = false)
    {
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        if (!ignoreCache &&
            cachedTextAssets.TryGetValue(path, out var textAsset) &&
            textAsset != null)
        {
            return textAsset;
        }

        var resource = LoadTextAssetResource(path);

        if(resource == null)
        {
            return null;
        }

        var outAsset = new TextAsset()
        {
            textResource = resource,
        };

        if (!ignoreCache)
        {
            cachedTextAssets.AddOrSetKey(path, outAsset);
        }

        return outAsset;
    }

    /// <summary>
    /// Locks an asset guid so it's not cleared when reloading the scene
    /// </summary>
    /// <param name="guid">The asset guid</param>
    public void LockAsset(string guid)
    {
        if(guid == null)
        {
            return;
        }

        var localGuid = AssetDatabase.GetAssetGuid(guid);

        if(localGuid != null)
        {
            lockedAssets.Add(localGuid.GetHashCode());
        }

        localGuid = AssetDatabase.GetAssetGuid(guid);

        if (localGuid != null)
        {
            lockedAssets.Add(localGuid.GetHashCode());
        }

        lockedAssets.Add(guid.GetHashCode());
    }

    public void ReloadMaterial(string guid)
    {
        if(!cachedMaterials.TryGetValue(guid, out var material))
        {
            return;
        }

        var resource = LoadMaterialResource(guid);

        if(resource != null)
        {
            material.materialResource = resource;
            material.CullingMode = resource.metadata.cullingMode;

            material.shaderKeywords.Clear();

            foreach (var variant in resource.metadata.enabledShaderVariants)
            {
                if (resource.shader.shaderResource.metadata.variants.Contains(variant))
                {
                    material.EnableShaderKeyword(variant);
                }
            }

            foreach (var parameter in resource.metadata.parameters)
            {
                switch (parameter.Value.type)
                {
                    case MaterialParameterType.TextureWrap:

                        material.materialResource.parameters.Add(parameter.Key, new()
                        {
                            name = parameter.Key,
                            type = MaterialParameterType.TextureWrap,
                            textureWrapValue = parameter.Value.textureWrapValue,
                        });

                        break;

                    case MaterialParameterType.Texture:

                        var texture = (parameter.Value.textureValue?.Length ?? 0) > 0 ? LoadTexture(parameter.Value.textureValue) : null;

                        material.SetTexture(parameter.Key, texture);

                        break;

                    case MaterialParameterType.Matrix3x3:

                        material.SetMatrix3x3(parameter.Key, new(), parameter.Value.source);

                        break;

                    case MaterialParameterType.Matrix4x4:

                        material.SetMatrix4x4(parameter.Key, new(), parameter.Value.source);

                        break;

                    case MaterialParameterType.Vector2:

                        material.SetVector2(parameter.Key, parameter.Value.vec2Value.ToVector2(), parameter.Value.source);

                        break;

                    case MaterialParameterType.Vector3:

                        material.SetVector3(parameter.Key, parameter.Value.vec3Value.ToVector3(), parameter.Value.source);

                        break;

                    case MaterialParameterType.Vector4:

                        material.SetVector4(parameter.Key, parameter.Value.vec4Value.ToVector4(), parameter.Value.source);

                        break;

                    case MaterialParameterType.Color:

                        material.SetColor(parameter.Key, parameter.Value.colorValue, parameter.Value.source);

                        break;

                    case MaterialParameterType.Float:

                        material.SetFloat(parameter.Key, parameter.Value.floatValue, parameter.Value.source);

                        break;

                    case MaterialParameterType.Int:

                        material.SetInt(parameter.Key, parameter.Value.intValue, parameter.Value.source);

                        break;
                }
            }
        }
    }

    public bool TryGetMaterial(string guid, out Material material) => cachedMaterials.TryGetValue(guid, out material);

    public Material GetMaterial(string guid)
    {
        return TryGetMaterial(guid, out var material) ? material : null;
    }

    public void ReloadShader(string guid)
    {
        if (!cachedShaders.TryGetValue(guid, out var shader))
        {
            return;
        }

        var resource = LoadShaderResource(guid);

        if (resource != null)
        {
            //TODO: Destroy
            /*
            if (shader.shaderResource != null)
            {
                shader.shaderResource.Destroy();
            }
            */

            shader.shaderResource = resource;
        }
    }

    public bool TryGetShader(string guid, out Shader shader) => cachedShaders.TryGetValue(guid, out shader);

    public Shader GetShader(string guid)
    {
        return TryGetShader(guid, out var shader) ? shader : null;
    }

    public void ReloadComputeShader(string guid)
    {
        if (!cachedComputeShaders.TryGetValue(guid, out var shader))
        {
            return;
        }

        var resource = LoadComputeShaderResource(guid);

        if (resource != null)
        {
            //TODO: Destroy
            /*
            if (shader.shaderResource != null)
            {
                shader.shaderResource.Destroy();
            }
            */

            shader.shaderResource = resource;
        }
    }

    public bool TryGetComputeShader(string guid, out ComputeShader shader) => cachedComputeShaders.TryGetValue(guid, out shader);

    public ComputeShader GetComputeShader(string guid)
    {
        return TryGetComputeShader(guid, out var shader) ? shader : null;
    }

    public void ReloadMeshAsset(string guid)
    {
        if (!cachedMeshAssets.TryGetValue(guid, out var mesh))
        {
            return;
        }

        var resource = LoadMeshAssetResource(guid);

        if (resource != null)
        {
            mesh.meshResource = resource;

            //TODO: Repopulate
        }
    }

    public bool TryGetMeshAsset(string guid, out MeshAsset mesh) => cachedMeshAssets.TryGetValue(guid, out mesh);

    public MeshAsset GetMeshAsset(string guid)
    {
        return TryGetMeshAsset(guid, out var meshAsset) ? meshAsset : null;
    }

    public void ReloadTextAsset(string guid)
    {
        if (!cachedTextAssets.TryGetValue(guid, out var textAsset))
        {
            return;
        }

        var resource = LoadTextAssetResource(guid);

        if (resource != null)
        {
            textAsset.textResource = resource;
        }
    }

    public bool TryGetTextAsset(string guid, out TextAsset textAsset) => cachedTextAssets.TryGetValue(guid, out textAsset);

    public TextAsset GetTextAsset(string guid)
    {
        return TryGetTextAsset(guid, out var textAsset) ? textAsset : null;
    }

    public void ReloadAudioClip(string guid)
    {
        if (!cachedAudioClips.TryGetValue(guid, out var audioClip))
        {
            return;
        }

        var resource = LoadAudioClipResource(guid);

        if (resource != null)
        {
            audioClip.audioResource = resource;
        }
    }

    public bool TryGetAudioClip(string guid, out AudioClip audioClip) => cachedAudioClips.TryGetValue(guid, out audioClip);

    public AudioClip GetAudioClip(string guid)
    {
        return TryGetAudioClip(guid, out var audioClip) ? audioClip : null;
    }

    public void ReloadFont(string guid)
    {
        if (!cachedFonts.TryGetValue(guid, out var font))
        {
            return;
        }

        var resource = LoadFontResource(guid);

        if (resource != null)
        {
            font.fontResource = resource;
        }
    }

    public bool TryGetFont(string guid, out FontAsset font) => cachedFonts.TryGetValue(guid, out font);

    public FontAsset GetFont(string guid)
    {
        return TryGetFont(guid, out var font) ? font : null;
    }

    public void ReloadTexture(string guid)
    {
        if (!cachedTextures.TryGetValue(guid, out var texture))
        {
            return;
        }

        var resource = LoadTextureResource(guid);

        if (resource != null)
        {
            texture.textureResource = resource;

            texture.ApplyTextureToSprites();
        }
    }

    public bool TryGetTexture(string guid, out Texture texture) => cachedTextures.TryGetValue(guid, out texture);

    public Texture GetTexture(string guid)
    {
        return TryGetTexture(guid, out var texture) ? texture : null;
    }
}
