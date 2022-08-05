using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

        public Scene LoadScene(string name)
        {
            var data = LoadFile(Path.Combine(basePath, "Scenes", $"{name}.stsc"));

            if(data == null)
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

                    foreach(var sceneObject in sceneData.objects)
                    {
                        switch(sceneObject.kind)
                        {
                            case SceneObjectKind.Entity:

                                {
                                    var entity = new Entity(sceneObject.name);

                                    scene.entities.Add(entity);

                                    var rotation = sceneObject.transform.rotation.ToVector3();

                                    entity.Transform.LocalPosition = sceneObject.transform.position.ToVector3();
                                    entity.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
                                    entity.Transform.LocalScale = sceneObject.transform.scale.ToVector3();

                                    if(sceneObject.parent != sceneObject.name)
                                    {
                                        entity.Transform.SetParent(sceneObject.parent != null ? scene.Find(sceneObject.parent)?.Transform : null);
                                    }

                                    foreach (var component in sceneObject.components)
                                    {
                                        var type = Type.GetType(component.type) ?? AppPlayer.active?.playerAssembly?.GetType(component.type);

                                        if(type == null)
                                        {
                                            continue;
                                        }

                                        var componentInstance = entity.AddComponent(type);

                                        if(componentInstance == null)
                                        {
                                            continue;
                                        }

                                        if(component.parameters != null)
                                        {
                                            foreach(var parameter in component.parameters)
                                            {
                                                if(parameter.name == null)
                                                {
                                                    continue;
                                                }

                                                try
                                                {
                                                    var field = type.GetField(parameter.name);

                                                    if (field != null)
                                                    {
                                                        switch (parameter.type)
                                                        {
                                                            case SceneComponentParameterType.Bool:

                                                                if (field.FieldType == typeof(bool))
                                                                {
                                                                    field.SetValue(componentInstance, parameter.boolValue);
                                                                }

                                                                break;

                                                            case SceneComponentParameterType.Float:

                                                                if (field.FieldType == typeof(float))
                                                                {
                                                                    field.SetValue(componentInstance, parameter.floatValue);
                                                                }
                                                                else if (field.FieldType == typeof(int))
                                                                {
                                                                    field.SetValue(componentInstance, (int)parameter.floatValue);
                                                                }

                                                                break;

                                                            case SceneComponentParameterType.Int:

                                                                if (field.FieldType == typeof(int))
                                                                {
                                                                    field.SetValue(componentInstance, parameter.intValue);
                                                                }
                                                                else if (field.FieldType == typeof(float))
                                                                {
                                                                    field.SetValue(componentInstance, parameter.intValue);
                                                                }

                                                                break;

                                                            case SceneComponentParameterType.String:

                                                                if (field.FieldType == typeof(string))
                                                                {
                                                                    field.SetValue(componentInstance, parameter.stringValue);

                                                                    continue;
                                                                }

                                                                if (field.FieldType.IsEnum)
                                                                {
                                                                    try
                                                                    {
                                                                        var value = Enum.Parse(field.FieldType, parameter.stringValue);

                                                                        if (value != null)
                                                                        {
                                                                            field.SetValue(componentInstance, value);
                                                                        }
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        continue;
                                                                    }

                                                                    continue;
                                                                }

                                                                if(field.FieldType == typeof(Color))
                                                                {
                                                                    //TODO

                                                                    continue;
                                                                }

                                                                if (field.FieldType == typeof(Material))
                                                                {
                                                                    var value = LoadMaterial(parameter.stringValue);

                                                                    if (value != null)
                                                                    {
                                                                        field.SetValue(componentInstance, value);
                                                                    }

                                                                    continue;
                                                                }

                                                                if (field.FieldType == typeof(Texture))
                                                                {
                                                                    var value = LoadTexture(parameter.stringValue);

                                                                    if (value != null)
                                                                    {
                                                                        field.SetValue(componentInstance, value);
                                                                    }

                                                                    continue;
                                                                }

                                                                break;
                                                        }
                                                    }
                                                }
                                                catch(Exception e)
                                                {
                                                    scene.RemoveEntity(entity);
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                        }
                    }

                    foreach(var entity in scene.entities)
                    {
                        foreach(var component in entity.components)
                        {
                            component.Invoke("OnAwake");
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

                    material = new Material();

                    material.shader = shader;

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
