using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Staple.Internal
{
    /// <summary>
    /// Resource manager. Keeps track of resources.
    /// </summary>
    internal class ResourceManager
    {
        /// <summary>
        /// Resource paths to load resources from
        /// </summary>
        public List<string> resourcePaths = new();

        private readonly Dictionary<string, Texture> cachedTextures = new();
        private readonly Dictionary<string, Material> cachedMaterials = new();
        private readonly Dictionary<string, Shader> cachedShaders = new();
        private readonly Dictionary<string, Mesh> cachedMeshes = new();
        private Dictionary<string, ResourcePak> resourcePaks = new();

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

                var stream = File.OpenRead(path);

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
        /// Destroys all resources
        /// </summary>
        internal void Destroy()
        {
            Material.WhiteTexture?.Destroy();

            foreach(var pair in cachedTextures)
            {
                pair.Value?.Destroy();
            }

            foreach (var pair in cachedMaterials)
            {
                pair.Value?.Destroy();
            }

            foreach (var pair in cachedShaders)
            {
                pair.Value?.Destroy();
            }

            foreach (var pair in cachedMeshes)
            {
                pair.Value?.Destroy();
            }

            foreach(var pair in Mesh.defaultMeshes)
            {
                pair.Value?.Destroy();
            }

            foreach(var pair in resourcePaks)
            {
                pair.Value.Dispose();
            }
        }

        /// <summary>
        /// Attempts to load a file as a byte array
        /// </summary>
        /// <param name="path">The path to load</param>
        /// <returns>The byte array, or null</returns>
        public byte[] LoadFile(string path)
        {
            var pakPath = path.Replace(Path.DirectorySeparatorChar, '/');

            foreach(var pair in resourcePaks)
            {
                var pak = pair.Value;

                foreach(var file in pak.Files)
                {
                    if(string.Equals(file.path, pakPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        using var stream = pak.OpenGuid(file.guid);

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
                return File.ReadAllBytes(path);
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

                foreach(var sceneObject in sceneObjects)
                {
                    var entity = Entity.Empty;

                    switch (sceneObject.kind)
                    {
                        case SceneObjectKind.Entity:

                            entity = scene.Instantiate(sceneObject, out var localID, false);

                            if (entity == Entity.Empty)
                            {
                                continue;
                            }

                            var transform = scene.GetComponent<Transform>(entity);

                            localIDs.Add(localID, transform);

                            if(sceneObject.parent >= 0)
                            {
                                parents.Add(localID, sceneObject.parent);
                            }

                            break;
                    }
                }

                foreach(var pair in parents)
                {
                    if(localIDs.TryGetValue(pair.Key, out var self) && localIDs.TryGetValue(pair.Value, out var parent))
                    {
                        self.SetParent(parent);
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
        /// Attempts to load a compiled scene from a path
        /// </summary>
        /// <param name="path">The path to load</param>
        /// <returns>The scene, or null</returns>
        public Scene LoadSceneFromPath(string path)
        {
            var data = LoadFile(path);

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
                var parents = new Dictionary<int, int>();

                foreach (var sceneObject in sceneData.objects)
                {
                    var entity = Entity.Empty;

                    switch (sceneObject.kind)
                    {
                        case SceneObjectKind.Entity:

                            entity = scene.Instantiate(sceneObject, out var localID, true);

                            if (entity == Entity.Empty)
                            {
                                continue;
                            }

                            var transform = scene.GetComponent<Transform>(entity);

                            localIDs.Add(localID, transform);

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
            return LoadSceneFromPath(Path.Combine("Scenes", $"{name}.stsc"));
        }

        /// <summary>
        /// Attempts to load a shader from a path
        /// </summary>
        /// <param name="path">The path to load</param>
        /// <returns>The shader, or null</returns>
        public Shader LoadShader(string path)
        {
            switch(RenderWindow.CurrentRenderer)
            {
                case RendererType.Direct3D11:

                    path = "d3d11/" + path;

                    break;

                case RendererType.Direct3D12:

                    path = "d3d12/" + path;

                    break;

                case RendererType.Metal:

                    path = "metal/" + path;

                    break;

                case RendererType.OpenGL:

                    path = "opengl/" + path;

                    break;

                case RendererType.OpenGLES:

                    path = "opengles/" + path;

                    break;

                case RendererType.Vulkan:

                    path = "vulkan/" + path;

                    break;
            }

            if (cachedShaders.TryGetValue(path, out var shader) && shader != null)
            {
                return shader;
            }

            var data = LoadFile(path);

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

                        if ((shaderData.computeShader?.Length ?? 0) == 0)
                        {
                            return null;
                        }

                        break;

                    case ShaderType.VertexFragment:

                        if ((shaderData.vertexShader?.Length ?? 0) == 0 || (shaderData.fragmentShader?.Length ?? 0) == 0)
                        {
                            return null;
                        }

                        break;
                }

                shader = Shader.Create(shaderData);

                if (shader != null)
                {
                    cachedShaders.Add(path, shader);
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
        /// <returns>The material, or null</returns>
        public Material LoadMaterial(string path)
        {
            if(cachedMaterials.TryGetValue(path, out var material) && material != null)
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

                if ((materialData.metadata.shaderPath?.Length ?? 0) == 0)
                {
                    Log.Error($"[ResourceManager] Failed to load material at path {path}: Invalid shader path");

                    return null;
                }

                var shader = LoadShader(materialData.metadata.shaderPath);

                if (shader == null)
                {
                    Log.Error($"[ResourceManager] Failed to load material at path {path}: Failed to load shader");

                    return null;
                }

                material = new Material
                {
                    shader = shader,
                    path = path
                };

                foreach(var parameter in materialData.metadata.parameters)
                {
                    switch(parameter.Key)
                    {
                        case Material.MainTextureProperty:

                            if(parameter.Value.textureValue != null)
                            {
                                material.MainTexture = LoadTexture(parameter.Value.textureValue);
                            }

                            break;

                        case Material.MainColorProperty:

                            material.MainColor = parameter.Value.colorValue;

                            break;
                    }
                }

                if (cachedMaterials.ContainsKey(path))
                {
                    cachedMaterials[path] = material;
                }
                else
                {
                    cachedMaterials.Add(path, material);
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
        /// <returns>The texture, or null</returns>
        public Texture LoadTexture(string path, TextureFlags flags = TextureFlags.None, byte skip = 0)
        {
            if(cachedTextures.TryGetValue(path, out var texture) && texture != null)
            {
                return texture;
            }

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

                if (cachedTextures.ContainsKey(path))
                {
                    cachedTextures[path] = texture;
                }
                else
                {
                    cachedTextures.Add(path, texture);
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
        /// Attempts to load a mesh from a path
        /// </summary>
        /// <param name="path">The path to the mesh file</param>
        /// <returns>The mesh, or null</returns>
        public Mesh LoadMesh(string path)
        {
            if(path.StartsWith("Internal/", StringComparison.InvariantCulture))
            {
                return Mesh.GetDefaultMesh(path);
            }

            return null;
        }
    }
}
