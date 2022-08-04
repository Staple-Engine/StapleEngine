using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class ResourceManager
    {
        public string basePath;

        public static ResourceManager instance = new ResourceManager();

        private Dictionary<string, Texture> cachedTextures = new Dictionary<string, Texture>();
        private Dictionary<string, Material> cachedMaterials = new Dictionary<string, Material>();
        private Dictionary<string, Shader> cachedShaders = new Dictionary<string, Shader>();
        
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

        public byte[] LoadFile(string path)
        {
            if(basePath == null)
            {
                return null;
            }

            try
            {
                return File.ReadAllBytes(Path.Combine(basePath, path));
            }
            catch(Exception)
            {
                return null;
            }
        }

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

                    material.shader = shader;

                    return material;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

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

                    texture = Texture.Create(textureData.data, textureData.metadata, flags, skip);

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
