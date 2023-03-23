using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        public List<string> resourcePaths = new List<string>();

        private Dictionary<string, Texture> cachedTextures = new Dictionary<string, Texture>();
        private Dictionary<string, Material> cachedMaterials = new Dictionary<string, Material>();
        private Dictionary<string, Shader> cachedShaders = new Dictionary<string, Shader>();

        /// <summary>
        /// The default instance of the resource manager
        /// </summary>
        public static ResourceManager instance = new ResourceManager();

        /// <summary>
        /// Destroys all resources
        /// </summary>
        internal void Destroy()
        {
            foreach(var pair in cachedTextures)
            {
                if(pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }

            foreach (var pair in cachedMaterials)
            {
                if (pair.Value != null)
                {
                    pair.Value.Destroy();
                }
            }

            foreach (var pair in cachedShaders)
            {
                if (pair.Value != null)
                {
                    pair.Value.Destroy();
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
            foreach(var resourcePath in resourcePaths)
            {
                try
                {
                    return File.ReadAllBytes(Path.Combine(resourcePath, path));
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

            using (var stream = new MemoryStream(data))
            {
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
                var sceneObjects = JsonConvert.DeserializeObject<List<SceneObject>>(data);
                var localIDs = new Dictionary<int, Transform>();
                var parents = new Dictionary<int, int>();

                foreach(var sceneObject in sceneObjects)
                {
                    var entity = Entity.Empty;

                    switch (sceneObject.kind)
                    {
                        case SceneObjectKind.Entity:

                            entity = scene.Instantiate(sceneObject, out var localID);

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
                return null;
            }

            var scene = new Scene();

            using (var stream = new MemoryStream(data))
            {
                try
                {
                    var header = MessagePackSerializer.Deserialize<SerializableSceneHeader>(stream);

                    if (header == null || header.header.SequenceEqual(SerializableSceneHeader.ValidHeader) == false ||
                        header.version != SerializableSceneHeader.ValidVersion)
                    {
                        return null;
                    }

                    var sceneData = MessagePackSerializer.Deserialize<SerializableScene>(stream);

                    if (sceneData == null || sceneData.objects == null)
                    {
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

                                entity = scene.Instantiate(sceneObject, out var localID);

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
                    return null;
                }
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
            if (cachedShaders.TryGetValue(path, out var shader) && shader != null)
            {
                return shader;
            }

            var data = LoadFile(path);

            if (data == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(data))
            {
                try
                {
                    var header = MessagePackSerializer.Deserialize<SerializableShaderHeader>(stream);

                    if (header == null || header.header.SequenceEqual(SerializableShaderHeader.ValidHeader) == false ||
                        header.version != SerializableShaderHeader.ValidVersion)
                    {
                        return null;
                    }

                    var shaderData = MessagePackSerializer.Deserialize<SerializableShader>(stream);

                    if (shaderData == null || shaderData.metadata == null)
                    {
                        return null;
                    }

                    switch(shaderData.metadata.type)
                    {
                        case ShaderType.Compute:

                            if((shaderData.computeShader?.Length ?? 0) == 0)
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

                    if(shader != null)
                    {
                        cachedShaders.Add(path, shader);
                    }

                    return shader;
                }
                catch (Exception e)
                {
                    return null;
                }
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
                return null;
            }

            using (var stream = new MemoryStream(data))
            {
                try
                {
                    var header = MessagePackSerializer.Deserialize<SerializableMaterialHeader>(stream);

                    if (header == null || header.header.SequenceEqual(SerializableMaterialHeader.ValidHeader) == false || header.version != SerializableMaterialHeader.ValidVersion)
                    {
                        return null;
                    }

                    var materialData = MessagePackSerializer.Deserialize<SerializableMaterial>(stream);

                    if(materialData == null || materialData.metadata == null)
                    {
                        return null;
                    }

                    if((materialData.metadata.shaderPath?.Length ?? 0) == 0)
                    {
                        return null;
                    }

                    var shader = LoadShader(materialData.metadata.shaderPath);

                    if(shader == null)
                    {
                        return null;
                    }

                    material = new Material();

                    material.shader = shader;
                    material.path = path;

                    if(cachedMaterials.ContainsKey(path))
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
                    return null;
                }
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
                return null;
            }

            using(var stream = new MemoryStream(data))
            {
                try
                {
                    var header = MessagePackSerializer.Deserialize<SerializableTextureHeader>(stream);

                    if(header == null || header.header.SequenceEqual(SerializableTextureHeader.ValidHeader) == false || header.version != SerializableTextureHeader.ValidVersion)
                    {
                        return null;
                    }

                    var textureData = MessagePackSerializer.Deserialize<SerializableTexture>(stream);

                    if(textureData == null)
                    {
                        return null;
                    }

                    texture = Texture.Create(path, textureData.data, textureData.metadata, flags, skip);

                    if (texture == null)
                    {
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
                    return null;
                }
            }
        }
    }
}
