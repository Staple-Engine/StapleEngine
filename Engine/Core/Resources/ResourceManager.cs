using MessagePack;
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

    /// <summary>
    /// Resource paths to load resources from
    /// </summary>
    public List<string> resourcePaths = [];

#if ANDROID
    internal Android.Content.Res.AssetManager assetManager;
#endif

    internal readonly Dictionary<string, Texture> cachedTextures = [];
    internal readonly Dictionary<string, Material> cachedMaterials = [];
    internal readonly Dictionary<string, Shader> cachedShaders = [];
    internal readonly Dictionary<string, Mesh> cachedMeshes = [];
    internal readonly Dictionary<string, AudioClip> cachedAudioClips = [];
    internal readonly Dictionary<string, MeshAsset> cachedMeshAssets = [];
    internal readonly Dictionary<string, FontAsset> cachedFonts = [];
    internal readonly Dictionary<string, IStapleAsset> cachedAssets = [];
    internal readonly Dictionary<string, Prefab> cachedPrefabs = [];
    internal readonly Dictionary<string, ResourcePak> resourcePaks = [];
    internal readonly List<WeakReference<Texture>> userCreatedTextures = [];
    internal readonly List<WeakReference<VertexBuffer>> userCreatedVertexBuffers = [];
    internal readonly List<WeakReference<IndexBuffer>> userCreatedIndexBuffers = [];

    /// <summary>
    /// Assets that must not be destroyed when using UserOnly mode
    /// </summary>
    internal readonly HashSet<int> lockedAssets = [];

    public static string ShaderPrefix
    {
        get
        {
            return RenderWindow.CurrentRenderer switch
            {
                RendererType.Direct3D11 => "d3d11/",
                RendererType.Direct3D12 => "d3d12/",
                RendererType.Metal => "metal/",
                RendererType.OpenGL => "opengl/",
                RendererType.OpenGLES => "opengles/",
                RendererType.Vulkan => "spirv/",
                _ => "",
            };
        }
    }

    /// <summary>
    /// The default instance of the resource manager
    /// </summary>
    public static ResourceManager instance = new();

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
                Log.Debug($"Attempted to load resource pak twice for path {path}");

                return false;
            }

#if ANDROID
            var s = assetManager.Open(path);

            var stream = new MemoryStream();

            s.CopyTo(stream);

            stream.Position = 0;
#else
            var stream = File.OpenRead(path);
#endif

            var resourcePak = new ResourcePak();

            if(resourcePak.Deserialize(stream) == false)
            {
                stream.Dispose();

                Console.WriteLine($"[Error] Failed to load resource pak at {path}: Likely invalid file");

                return false;
            }

            resourcePaks.Add(path, resourcePak);

            return true;
        }
        catch(Exception e)
        {
            Console.WriteLine($"[Error] Failed to load resource pak at {path}: {e}");

            return false;
        }
    }

    /// <summary>
    /// Clears all assets that are not locked
    /// </summary>
    internal void Clear()
    {
        Destroy(DestroyMode.UserOnly);

        var destroyed = new HashSet<string>();

        foreach (var pair in cachedTextures)
        {
            if(lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMaterials)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedShaders)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMeshes)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedAudioClips)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedMeshAssets)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedFonts)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedAssets)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach (var pair in cachedPrefabs)
        {
            if (lockedAssets.Contains(pair.Key.GetHashCode()) == false)
            {
                destroyed.Add(pair.Key);
            }
        }

        foreach(var key in destroyed)
        {
            cachedTextures.Remove(key);
            cachedMaterials.Remove(key);
            cachedShaders.Remove(key);
            cachedMeshes.Remove(key);
            cachedAudioClips.Remove(key);
            cachedMeshAssets.Remove(key);
            cachedFonts.Remove(key);
            cachedAssets.Remove(key);
            cachedPrefabs.Remove(key);
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
            Material.WhiteTexture?.Destroy();
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
            if(item.TryGetTarget(out var buffer) && buffer.Disposed == false)
            {
                buffer.Destroy();
            }
        }

        foreach (var item in userCreatedIndexBuffers)
        {
            if (item.TryGetTarget(out var buffer) && buffer.Disposed == false)
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
    /// Attempts to recreate resources (used usually when context is lost and bgfx was restarted)
    /// </summary>
    internal void RecreateResources()
    {
        Log.Debug("Recreating resources");

        try
        {
            if(Material.WhiteTexture?.Create() ?? false)
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
                if(pair.Value?.Create() ?? false)
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
                if (texture.Create() == false)
                {
                    Log.Debug($"Failed to recreate a user texture");
                }
            }
        }

        foreach (var pair in cachedShaders)
        {
            try
            {
                if (pair.Value?.Create() ?? false)
                {
                    Log.Debug($"Recreated shader {pair.Key}");
                }
            }
            catch (Exception e)
            {
                Log.Debug($"Failed to recreate shader: {e}");
            }
        }

        foreach (var pair in cachedMaterials)
        {
            if(pair.Value == null)
            {
                continue;
            }

            pair.Value.Disposed = false;
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
    public byte[] LoadFile(string path, string prefix = null)
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
            var localPath = prefix != null ? AssetDatabase.GetAssetPath(guid, prefix) : AssetDatabase.GetAssetPath(guid);

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

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SceneListHeader>(stream);

            if (header == null || header.header.SequenceEqual(SceneListHeader.ValidHeader) == false ||
                header.version != SceneListHeader.ValidVersion)
            {
                return null;
            }

            var sceneData = MessagePackSerializer.Deserialize<SceneList>(stream);

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
        if ((path?.Length ?? 0) == 0)
        {
            return null;
        }

        World.Current = new();

        Scene.current = null;

        var data = LoadFileString(path);

        if (data == null)
        {
            return null;
        }

        var scene = new Scene();

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

                    if(World.Current?.TryGetEntity(self.entity, out var entityInfo) ?? false)
                    {
                        entityInfo.enabledInHierarchy = parent.entity.EnabledInHierarchy;
                    }
                }
            }

            foreach(var pair in localIDs)
            {
                var entity = pair.Value.entity;
                var sceneObject = sceneObjects[localSceneObjects[pair.Key]];

                foreach(var component in sceneObject.components)
                {
                    var componentType = TypeCache.GetType(component.type);

                    if(componentType == null ||
                        entity.TryGetComponent(componentType, out var componentInstance) == false)
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
                                        targetComponentType.IsAssignableTo(field.FieldType) == false)
                                    {
                                        continue;
                                    }

                                    var targetEntity = Scene.FindEntity(entityID);

                                    if (targetEntity.IsValid == false ||
                                        targetEntity.TryGetComponent(targetComponentType, out var targetComponent) == false)
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

            return scene;
        }
        catch (Exception e)
        {
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
        var path = AssetDatabase.GetAssetPath(guid);

        if(path == null)
        {
            return null;
        }

        var data = LoadFile(guid);

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load scene at path {path}");

            return null;
        }

        var scene = new Scene();

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableSceneHeader>(stream);

            if (header == null || header.header.SequenceEqual(SerializableSceneHeader.ValidHeader) == false ||
                header.version != SerializableSceneHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load scene at path {path}: Invalid header");

                return null;
            }

            var sceneData = MessagePackSerializer.Deserialize<SerializableScene>(stream);

            if (sceneData == null || sceneData.objects == null)
            {
                Log.Error($"[ResourceManager] Failed to load scene at path {path}: Invalid scene data");

                return null;
            }

            var localIDs = new Dictionary<int, Transform>();
            var localSceneObjects = new Dictionary<int, int>();
            var parents = new Dictionary<int, int>();

            for(var i = 0; i < sceneData.objects.Count; i++)
            {
                var sceneObject = sceneData.objects[i];
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
                var entity = pair.Value.entity;
                var sceneObject = sceneData.objects[localSceneObjects[pair.Key]];

                foreach (var component in sceneObject.components)
                {
                    var componentType = TypeCache.GetType(component.type);

                    if (componentType == null ||
                        entity.TryGetComponent(componentType, out var componentInstance) == false)
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
                                        targetComponentType.IsAssignableTo(field.FieldType) == false)
                                    {
                                        continue;
                                    }

                                    var targetEntity = Scene.FindEntity(entityID);

                                    if (targetEntity.IsValid == false ||
                                        targetEntity.TryGetComponent(targetComponentType, out var targetComponent) == false)
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
                var entity = pair.Value.entity;

                entity.IterateComponents((ref IComponent c) =>
                {
                    World.Current?.EmitAddComponentEvent(entity, ref c);
                });
            }

            return scene;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load scene at path {path}: {e}");

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
                Log.Debug($"[ResourceManager] Failed to load scene {name}: Failed to find asset");
            }
            else
            {
                Log.Debug($"[ResourceManager] Failed to load scene {name}: Invalid asset type {assetType}");
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
            return null;
        }

        var prefix = ShaderPrefix;
        var guid = AssetDatabase.GetAssetGuid(path);

        if (path.StartsWith(prefix) == false)
        {
            path = prefix + path;
        }

        guid = AssetDatabase.GetAssetGuid(path) ?? guid;

        if (guid == null)
        {
            return null;
        }

        byte[] data = LoadFile(guid, ShaderPrefix);

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load shader at guid {guid}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableShaderHeader>(stream);

            if (header == null || header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) == false ||
                header.version != SerializableShaderHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load shader at guid {guid}: Invalid header");

                return null;
            }

            var shaderData = MessagePackSerializer.Deserialize<SerializableShader>(stream);

            if (shaderData == null || shaderData.metadata == null)
            {
                Log.Error($"[ResourceManager] Failed to load shader at guid {guid}: Invalid shader data");

                return null;
            }

            return shaderData;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load shader at guid {guid}: {e}");

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

        var prefix = ShaderPrefix;
        var guid = AssetDatabase.GetAssetGuid(path);

        if (path.StartsWith(prefix) == false)
        {
            path = prefix + path;
        }

        guid = AssetDatabase.GetAssetGuid(path) ?? guid;

        if(guid == null)
        {
            return null;
        }

        if (ignoreCache == false &&
            cachedShaders.TryGetValue(path, out var shader) &&
            shader != null &&
            shader.Disposed == false)
        {
            return shader;
        }

        byte[] data = LoadFile(guid, ShaderPrefix);

        if(data == null)
        {
            data = LoadFile(path);
        }

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load shader at path {path}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableShaderHeader>(stream);

            if (header == null || header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) == false ||
                header.version != SerializableShaderHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load shader at path {path}: Invalid header");

                return null;
            }

            var shaderData = MessagePackSerializer.Deserialize<SerializableShader>(stream);

            if (shaderData == null || shaderData.metadata == null)
            {
                Log.Error($"[ResourceManager] Failed to load shader at path {path}: Invalid shader data");

                return null;
            }

            switch (shaderData.metadata.type)
            {
                case ShaderType.Compute:

                    foreach(var pair in shaderData.data)
                    {
                        if ((pair.Value.computeShader?.Length ?? 0) == 0)
                        {
                            return null;
                        }
                    }

                    break;

                case ShaderType.VertexFragment:

                    foreach (var pair in shaderData.data)
                    {
                        if ((pair.Value.vertexShader?.Length ?? 0) == 0 || (pair.Value.fragmentShader?.Length ?? 0) == 0)
                        {
                            return null;
                        }
                    }

                    break;
            }

            shader = Shader.Create(shaderData);

            if (shader != null)
            {
                shader.Guid.Guid = guid;

                if(ignoreCache == false)
                {
                    cachedShaders.AddOrSetKey(path, shader);
                }
            }

            return shader;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load shader at path {path}: {e}");

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

        if (ignoreCache == false &&
            cachedMaterials.TryGetValue(path, out var material) &&
            material != null &&
            material.Disposed == false)
        {
            return material;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load material at path {path}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableMaterialHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableMaterialHeader.ValidHeader) == false ||
                header.version != SerializableMaterialHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load material at path {path}: Invalid header");

                return null;
            }

            var materialData = MessagePackSerializer.Deserialize<SerializableMaterial>(stream);

            if (materialData == null || materialData.metadata == null)
            {
                Log.Error($"[ResourceManager] Failed to load material at path {path}: Invalid data");

                return null;
            }

            if ((materialData.metadata.shader?.Length ?? 0) == 0)
            {
                Log.Error($"[ResourceManager] Failed to load material at path {path}: Invalid shader path");

                return null;
            }

            var shader = LoadShader(materialData.metadata.shader, ignoreCache);

            if (shader == null)
            {
                Log.Error($"[ResourceManager] Failed to load material at path {path}: Failed to load shader");

                return null;
            }

            var guid = AssetDatabase.GetAssetGuid(path);

            material = new Material
            {
                metadata = materialData.metadata,
                shader = shader,
                CullingMode = materialData.metadata.cullingMode,
            };

            material.Guid.Guid = guid ?? path;

            foreach(var variant in materialData.metadata.enabledShaderVariants)
            {
                if(shader.metadata.variants.Contains(variant))
                {
                    material.EnableShaderKeyword(variant);
                }
            }

            foreach(var parameter in materialData.metadata.parameters)
            {
                switch(parameter.Value.type)
                {
                    case MaterialParameterType.TextureWrap:

                        material.parameters.Add(parameter.Key.GetHashCode(), new()
                        {
                            name = parameter.Key,
                            type = MaterialParameterType.TextureWrap,
                            textureWrapValue = parameter.Value.textureWrapValue,
                        });

                        break;

                    case MaterialParameterType.Texture:

                        var texture = (parameter.Value.textureValue?.Length ?? 0) > 0 ? LoadTexture(parameter.Value.textureValue) : null;

                        if (parameter.Key == Material.MainTextureProperty)
                        {
                            material.MainTexture = texture;
                        }
                        else
                        {
                            material.SetTexture(parameter.Key, texture);
                        }

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

                        if(parameter.Key == Material.MainColorProperty)
                        {
                            material.MainColor = parameter.Value.colorValue;
                        }
                        else
                        {
                            material.SetColor(parameter.Key, parameter.Value.colorValue, parameter.Value.source);
                        }

                        break;

                    case MaterialParameterType.Float:

                        material.SetFloat(parameter.Key, parameter.Value.floatValue, parameter.Value.source);

                        break;

                    case MaterialParameterType.Int:

                        material.SetInt(parameter.Key, parameter.Value.intValue, parameter.Value.source);

                        break;
                }
            }

            if(ignoreCache == false)
            {
                cachedMaterials.AddOrSetKey(path, material);
            }

            return material;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load material at path {path}: {e}");

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a texture from a path
    /// </summary>
    /// <param name="path">The path to load</param>
    /// <param name="flags">Any additional texture flags</param>
    /// <param name="skip">Skip top level mips</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The texture, or null</returns>
    public Texture LoadTexture(string path, TextureFlags flags = TextureFlags.None, byte skip = 0, bool ignoreCache = false)
    {
        if((path?.Length ?? 0) == 0)
        {
            return null;
        }

        if(ignoreCache == false &&
            cachedTextures.TryGetValue(path, out var texture) &&
            texture != null &&
            texture.Disposed == false)
        {
            return texture;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        var data = LoadFile(path);

        if(data == null)
        {
            Log.Error($"[ResourceManager] Failed to load texture at path {path}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableTextureHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) == false ||
                header.version != SerializableTextureHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load texture at path {path}: Invalid header");

                return null;
            }

            var textureData = MessagePackSerializer.Deserialize<SerializableTexture>(stream);

            if (textureData == null)
            {
                Log.Error($"[ResourceManager] Failed to load texture at path {path}: Invalid texture data");

                return null;
            }

            texture = Texture.Create(path, textureData.data, textureData.metadata, flags, skip);

            if (texture == null)
            {
                Log.Error($"[ResourceManager] Failed to load texture at path {path}: Failed to create texture");

                return null;
            }

            if(textureData.cpuData != null)
            {
                texture.readbackData = new()
                {
                    colorComponents = textureData.cpuData.colorComponents,
                    data = textureData.cpuData.data,
                    width = textureData.cpuData.width,
                    height = textureData.cpuData.height,
                };
            }

            if(ignoreCache == false)
            {
                cachedTextures.AddOrSetKey(path, texture);
            }

            return texture;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load texture at path {path}: {e}");

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

        if (ignoreCache == false &&
            cachedAudioClips.TryGetValue(path, out var audioClip) &&
            audioClip != null)
        {
            return audioClip;
        }

        var guid = AssetDatabase.GetAssetGuid(path);

        path = guid ?? path;

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load audio clip at path {path}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableAudioClipHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableAudioClipHeader.ValidHeader) == false ||
                header.version != SerializableAudioClipHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load audio clip at path {path}: Invalid header");

                return null;
            }

            var audioData = MessagePackSerializer.Deserialize<SerializableAudioClip>(stream);

            if (audioData == null)
            {
                Log.Error($"[ResourceManager] Failed to load audio clip at path {path}: Invalid audio data");

                return null;
            }

            audioClip = new AudioClip()
            {
                metadata = audioData.metadata,
                fileData = audioData.fileData,
                format = audioData.format,
            };

            audioClip.Guid.Guid = path;

            if(ignoreCache == false)
            {
                cachedAudioClips.AddOrSetKey(path, audioClip);
            }

            return audioClip;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load audio clip at path {path}: {e}");

            return null;
        }
    }

    /// <summary>
    /// Attempts to load a mesh from a path
    /// </summary>
    /// <param name="guid">The guid to the mesh file. This guid can have a special terminator to indicate the mesh index (guid:index)</param>
    /// <param name="ignoreCache">Whether to ignore the cache</param>
    /// <returns>The mesh, or null</returns>
    public Mesh LoadMesh(string guid, bool ignoreCache = false)
    {
        if ((guid?.Length ?? 0) == 0)
        {
            return null;
        }

        if (guid.StartsWith("Internal/", StringComparison.InvariantCulture))
        {
            return Mesh.GetDefaultMesh(guid);
        }

        var original = guid;

        var index = 0;

        if(guid.Contains(':'))
        {
            var split = guid.Split(':');

            if(split.Length != 2 || int.TryParse(split[1], out index) == false || index < 0)
            {
                return null;
            }

            guid = split[0];
        }

        if (ignoreCache == false &&
            cachedMeshes.TryGetValue(original, out var mesh) &&
            mesh != null)
        {
            return mesh;
        }

        var asset = LoadMeshAsset(guid);

        if(asset == null ||
            (asset.meshes.Count > 0 &&
            index >= asset.meshes.Count))
        {
            return null;
        }

        if (asset.meshes.Count > 0)
        {
            var m = asset.meshes[index];

            mesh = new Mesh(true, false)
            {
                vertices = m.vertices,
                normals = m.normals,
                tangents = m.tangents,
                bitangents = m.bitangents,

                uv = m.UV1,
                uv2 = m.UV2,
                uv3 = m.UV3,
                uv4 = m.UV4,
                uv5 = m.UV5,
                uv6 = m.UV6,
                uv7 = m.UV7,
                uv8 = m.UV8,

                indices = m.indices,

                boneIndices = m.boneIndices,
                boneWeights = m.boneWeights,

                meshTopology = m.topology,
                indexFormat = MeshIndexFormat.UInt32,
                bounds = m.bounds,

                meshAsset = asset,
                meshAssetIndex = index,
            };

            if (m.colors.Length > 0)
            {
                mesh.colors = m.colors;
            }

            if (m.colors2.Length > 0)
            {
                mesh.colors2 = m.colors2;
            }

            if (m.colors3.Length > 0)
            {
                mesh.colors3 = m.colors3;
            }

            if (m.colors4.Length > 0)
            {
                mesh.colors4 = m.colors4;
            }

            foreach (var submesh in m.submeshes)
            {
                mesh.AddSubmesh(submesh.startVertex, submesh.vertexCount, submesh.startIndex, submesh.indexCount, m.topology);
            }

            mesh.changed = true;
        }
        else
        {
            mesh = new Mesh(true, false)
            {
                meshAsset = asset,
            };
        }

        mesh.Guid.Guid = (original.Contains('/') || original.Contains('\\')) ? $"{asset.Guid}:{index}" : original;

        if(ignoreCache == false)
        {
            cachedMeshes.AddOrSetKey(original, mesh);
        }

        return mesh;
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

        if (ignoreCache == false &&
            cachedMeshAssets.TryGetValue(path, out var mesh) &&
            mesh != null)
        {
            return mesh;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            Log.Error($"[ResourceManager] Failed to load mesh asset at path {path}");

            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableMeshAssetHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableMeshAssetHeader.ValidHeader) == false ||
                header.version != SerializableMeshAssetHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load mesh asset at path {path}: Invalid header");

                return null;
            }

            var meshAssetData = MessagePackSerializer.Deserialize<SerializableMeshAsset>(stream);

            if (meshAssetData == null)
            {
                Log.Error($"[ResourceManager] Failed to load mesh asset at path {path}: Invalid mesh asset data");

                return null;
            }

            var asset = new MeshAsset()
            {
                lighting = meshAssetData.metadata.lighting,
                frameRate = meshAssetData.metadata.frameRate,
                syncAnimationToRefreshRate = meshAssetData.metadata.syncAnimationToRefreshRate,
            };

            asset.Guid.Guid = guid;

            asset.nodes = new MeshAsset.Node[meshAssetData.nodes.Length];

            for (var i = 0; i < meshAssetData.nodes.Length; i++)
            {
                var node = meshAssetData.nodes[i];

                var transform = Math.TransformationMatrix(node.position.ToVector3(), node.scale.ToVector3(), node.rotation.ToQuaternion());

                asset.nodes[i] = new MeshAsset.Node()
                {
                    name = node.name,
                    index = i,
                    Transform = transform,
                    OriginalTransform = transform,
                    meshIndices = node.meshIndices.ToArray(),
                    children = node.children.ToArray(),
                };
            }

            for (var i = 0; i < meshAssetData.nodes.Length; i++)
            {
                var node = meshAssetData.nodes[i];

                asset.nodes[i].parent = asset.nodes.FirstOrDefault(x => x.children.Contains(i));
            }

            var startBoneIndex = 0;

            foreach(var m in meshAssetData.meshes)
            {
                var newMesh = new MeshAsset.MeshInfo()
                {
                    name = m.name,
                    topology = m.topology,
                    lighting = asset.lighting,
                    type = m.type,

                    bounds = new AABB(m.boundsCenter.ToVector3(), m.boundsExtents.ToVector3()),

                    vertices = m.vertices
                        .Select(x => x.ToVector3())
                        .ToArray(),

                    normals = m.normals
                        .Select(x => x.ToVector3())
                        .ToArray(),

                    colors = m.colors
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        }).ToArray(),

                    colors2 = m.colors2
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        }).ToArray(),

                    colors3 = m.colors3
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        }).ToArray(),

                    colors4 = m.colors4
                        .Select(x =>
                        {
                            var v = x.ToVector4();

                            return new Color(v.X, v.Y, v.Z, v.W);
                        }).ToArray(),

                    tangents = m.tangents
                        .Select(x => x.ToVector3())
                        .ToArray(),

                    bitangents = m.bitangents
                        .Select(x => x.ToVector3())
                        .ToArray(),

                    UV1 = m.UV1
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV2 = m.UV2
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV3 = m.UV3
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV4 = m.UV4
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV5 = m.UV5
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV6 = m.UV6
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV7 = m.UV7
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    UV8 = m.UV8
                        .Select(x => x.ToVector2())
                        .ToArray(),

                    indices = m.indices.ToArray(),

                    boneIndices = m.boneIndices
                        .Select(x => x.ToVector4())
                        .ToArray(),

                    boneWeights = m.boneWeights
                        .Select(x => x.ToVector4())
                        .ToArray(),

                    startBoneIndex = startBoneIndex,

                    bones = [m.bones.Select(x => new MeshAsset.Bone()
                    {
                        nodeIndex = x.nodeIndex,
                        offsetMatrix = Math.TransformationMatrix(x.offsetPosition.ToVector3(), x.offsetScale.ToVector3(), x.offsetRotation.ToQuaternion()),
                    }).ToArray()],
                };

                for (var i = 0; i < newMesh.boneIndices.Length; i++)
                {
                    newMesh.boneIndices[i] += new Vector4(startBoneIndex);
                }

                startBoneIndex += newMesh.bones[0].Length;

                newMesh.submeshes = [new()
                {
                    startVertex = 0,
                    startIndex = 0,
                    vertexCount = m.vertices.Count,
                    indexCount = m.indices.Count,
                }];

                newMesh.submeshMaterialGuids = [ m.materialGuid ];

                asset.meshes.Add(newMesh);
            }

            asset.BoneCount = startBoneIndex;

            if(asset.meshes.Count == 1)
            {
                asset.Bounds = asset.meshes[0].bounds;
            }
            else if(asset.meshes.Count > 0)
            {
                var min = Vector3.One * 999999;
                var max = Vector3.One * -999999;

                foreach(var m in asset.meshes)
                {
                    if(min.X > m.bounds.center.X)
                    {
                        min.X = m.bounds.center.X;
                    }

                    if (min.Y > m.bounds.center.Y)
                    {
                        min.Y = m.bounds.center.Y;
                    }

                    if (min.Z > m.bounds.center.Z)
                    {
                        min.Z = m.bounds.center.Z;
                    }

                    if (max.X < m.bounds.center.X + m.bounds.size.X)
                    {
                        max.X = m.bounds.center.X + m.bounds.size.X;
                    }

                    if (max.Y < m.bounds.center.Y + m.bounds.size.Y)
                    {
                        max.Y = m.bounds.center.Y + m.bounds.size.Y;
                    }

                    if (max.Z < m.bounds.center.Z + m.bounds.size.Z)
                    {
                        max.Z = m.bounds.center.Z + m.bounds.size.Z;
                    }
                }

                asset.Bounds = AABB.CreateFromMinMax(min, max);
            }

            foreach(var a in meshAssetData.animations)
            {
                var animation = new MeshAsset.Animation()
                {
                    name = a.name,
                    duration = a.duration,
                };

                foreach(var c in a.channels)
                {
                    var channel = new MeshAsset.AnimationChannel()
                    {
                        nodeIndex = c.nodeIndex,
                        positions = c.positionKeys.Select(x => new MeshAsset.AnimationKey<Vector3>()
                        {
                            time = x.time,
                            value = x.value.ToVector3(),
                        }).ToList(),

                        rotations = c.rotationKeys.Select(x => new MeshAsset.AnimationKey<Quaternion>()
                        {
                            time = x.time,
                            value = new Quaternion(x.value.x, x.value.y, x.value.z, x.value.w),
                        }).ToList(),

                        scales = c.scaleKeys.Select(x => new MeshAsset.AnimationKey<Vector3>()
                        {
                            time = x.time,
                            value = x.value.ToVector3(),
                        }).ToList(),
                    };

                    animation.channels.Add(channel);
                }

                asset.animations.AddOrSetKey(animation.name, animation);
            }

            if(ignoreCache == false)
            {
                cachedMeshAssets.AddOrSetKey(path, asset);
            }

            return asset;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load mesh asset at path {path}: {e}");

            return null;
        }
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
            Log.Error($"[ResourceManager] Failed to load asset at path {path}: Type {value.GetType().FullName} is not matching requested type {typeof(T).FullName}");

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

        if (ignoreCache == false &&
            cachedAssets.TryGetValue(path, out var asset) &&
            asset != null)
        {
            return asset;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            return default;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) == false ||
                header.version != SerializableStapleAssetHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load asset at path {path}: Invalid header");

                return default;
            }

            var assetBundle = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

            if (assetBundle == null)
            {
                Log.Error($"[ResourceManager] Failed to load asset at path {path}: Invalid asset data");

                return default;
            }

            asset = AssetSerialization.Deserialize(assetBundle, StapleSerializationMode.Binary);

            if (asset != null)
            {
                if(asset is IGuidAsset guidAsset)
                {
                    guidAsset.Guid.Guid = path;
                }

                if(ignoreCache == false)
                {
                    cachedAssets.AddOrSetKey(path, asset);
                }
            }

            return asset;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load asset at path {path}: {e}");

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

        if (data == null)
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

        if (ignoreCache == false &&
            cachedPrefabs.TryGetValue(path, out var prefab) &&
            prefab != null)
        {
            return prefab;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            return default;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializablePrefabHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializablePrefabHeader.ValidHeader) == false ||
                header.version != SerializablePrefabHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load prefab at path {path}: Invalid header");

                return default;
            }

            var prefabData = MessagePackSerializer.Deserialize<SerializablePrefab>(stream);

            if (prefabData == null)
            {
                Log.Error($"[ResourceManager] Failed to load prefab at path {path}: Invalid prefab data");

                return default;
            }

            prefab = new()
            {
                data = prefabData,
            };

            prefab.Guid.Guid = guid;

            if (ignoreCache == false)
            {
                cachedPrefabs.AddOrSetKey(path, prefab);
            }

            return prefab;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load prefab at path {path}: {e}");

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

        if (ignoreCache == false &&
            cachedFonts.TryGetValue(path, out var font) &&
            font != null)
        {
            return font;
        }

        var data = LoadFile(path);

        if (data == null)
        {
            return default;
        }

        using var stream = new MemoryStream(data);

        try
        {
            var header = MessagePackSerializer.Deserialize<SerializableFontHeader>(stream);

            if (header == null ||
                header.header.SequenceEqual(SerializableFontHeader.ValidHeader) == false ||
                header.version != SerializableFontHeader.ValidVersion)
            {
                Log.Error($"[ResourceManager] Failed to load font at path {path}: Invalid header");

                return default;
            }

            var fontData = MessagePackSerializer.Deserialize<SerializableFont>(stream);

            if (fontData == null)
            {
                Log.Error($"[ResourceManager] Failed to load font at path {path}: Invalid font data");

                return default;
            }

            font = new()
            {
                metadata = fontData.metadata,
            };

            font.Guid.Guid = path;

            font.font = TextFont.FromData(fontData.fontData, font.Guid.Guid, fontData.metadata.useAntiAliasing,
                fontData.metadata.textureSize, fontData.metadata.includedCharacterSets);

            if (ignoreCache == false)
            {
                cachedFonts.AddOrSetKey(path, font);
            }

            return font;
        }
        catch (Exception e)
        {
            Log.Error($"[ResourceManager] Failed to load font at path {path}: {e}");

            return default;
        }
    }

    /// <summary>
    /// Locks an asset guid so it's not cleared when reloading the scene
    /// </summary>
    /// <param name="guid">The asset guid</param>
    public void LockAsset(string guid)
    {
        var localGuid = AssetDatabase.GetAssetGuid(guid);

        if(localGuid != null)
        {
            lockedAssets.Add(localGuid.GetHashCode());
        }

        localGuid = AssetDatabase.GetAssetGuid(guid, ShaderPrefix);

        if (localGuid != null)
        {
            lockedAssets.Add(localGuid.GetHashCode());
        }

        lockedAssets.Add(guid.GetHashCode());
    }
}
