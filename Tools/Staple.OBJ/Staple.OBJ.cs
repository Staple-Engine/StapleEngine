using JeremyAnsel.Media.WavefrontObj;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Tooling;

public class OBJImporter : IMeshImporter
{
    public bool HandlesExtension(string extension) => extension == ".obj";

    public SerializableMeshAsset ImportMesh(MeshImporterContext context)
    {
        var (metadata, meshFileName, inputPath, standardShader, ShaderHasParameter, meshMaterialLock, processedTextures,
            resolveTexturePath) = (context.metadata, context.meshFileName, context.inputPath, context.standardShader,
            context.shaderHasParameter, context.materialLock, context.processedTextures, context.resolveTexturePath);

        ObjFile model = null;

        var materials = new List<ObjMaterialFile>();

        try
        {
            model = ObjFile.FromFile(meshFileName);
        }
        catch(Exception e)
        {
            Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}: {e}");

            return null;
        }

        if (model == null || model.Faces.Count == 0)
        {
            Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}");

            return null;
        }

        foreach (var library in model.MaterialLibraries)
        {
            try
            {
                var material = ObjMaterialFile.FromFile($"{Path.GetDirectoryName(meshFileName)}/{library}");

                if(material != null)
                {
                    materials.Add(material);
                }
            }
            catch(Exception e)
            {
            }
        }

        if (metadata.frameRate <= 0)
        {
            metadata.frameRate = 30;
        }

        var node = new MeshAssetNode()
        {
            name = "root",
            position = new(),
            rotation = new(Quaternion.Identity),
            scale = new(Vector3.One),
        };

        var meshData = new SerializableMeshAsset
        {
            metadata = metadata,
            nodes = [node],
        };

        #region Materials
        var materialMapping = new List<string>();
        var materialNames = new List<string>();

        if (materials != null)
        {
            lock (meshMaterialLock)
            {
                var counter = 0;

                foreach (var material in materials)
                {
                    foreach (var m in material.Materials)
                    {
                        var baseName = string.IsNullOrEmpty(m.Name) == false ? m.Name : (++counter).ToString();

                        baseName = string.Join('_', baseName.Split(Path.GetInvalidFileNameChars()));

                        var fileName = $"{baseName}.{AssetSerialization.MaterialExtension}";

                        var target = Path.Combine(Path.GetDirectoryName(meshFileName), fileName);
                        var materialGuid = Utilities.FindGuid<Material>($"{target}.meta");

                        materialMapping.Add(materialGuid);
                        materialNames.Add(m.Name);

                        try
                        {
                            if (File.Exists(target))
                            {
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                        }

                        var materialMetadata = new MaterialMetadata()
                        {
                            shader = standardShader.metadata.guid,
                        };

                        var basePath = Path.GetDirectoryName(meshFileName).Replace(inputPath, "");

                        if (basePath.Length > 0)
                        {
                            basePath = basePath[1..];
                        }

                        foreach (var p in standardShader?.metadata?.instanceParameters ?? [])
                        {
                            switch (p.type)
                            {
                                case ShaderUniformType.Color:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Color,
                                        source = MaterialParameterSource.Instance,
                                        colorValue = Color.White,
                                    });

                                    break;

                                case ShaderUniformType.Int:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Int,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Float:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Float,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector2:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector2,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector3:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector3,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;

                                case ShaderUniformType.Vector4:

                                    materialMetadata.parameters.Add(p.name, new()
                                    {
                                        type = MaterialParameterType.Vector4,
                                        source = MaterialParameterSource.Instance,
                                    });

                                    break;
                            }
                        }

                        void AddColor(string name, bool has, ObjMaterialColor color)
                        {
                            if (ShaderHasParameter(name) == false)
                            {
                                return;
                            }

                            var c = Color.White;

                            if (has)
                            {
                                c.r = color.Color.X;
                                c.g = color.Color.Y;
                                c.b = color.Color.Z;
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Color,
                                source = MaterialParameterSource.Uniform,
                                colorValue = c,
                            });
                        }

                        AddColor("ambientColor", m.AmbientColor != null, m.AmbientColor);
                        AddColor("diffuseColor", m.DiffuseColor != null, m.DiffuseColor);
                        AddColor("emissiveColor", m.EmissiveColor != null, m.EmissiveColor);
                        AddColor("specularColor", m.SpecularColor != null, m.SpecularColor);

                        void AddTexture(string name, bool has, string fileName)
                        {
                            if (ShaderHasParameter(name) == false)
                            {
                                return;
                            }

                            var texturePath = "";

                            if (has)
                            {
                                texturePath = resolveTexturePath(fileName, meshFileName);
                            }

                            if (ShaderHasParameter($"{name}_UMapping") && ShaderHasParameter($"{name}_VMapping"))
                            {
                                materialMetadata.parameters.Add($"{name}_UMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    source = MaterialParameterSource.Uniform,
                                    textureWrapValue = TextureWrap.Clamp,
                                });

                                materialMetadata.parameters.Add($"{name}_VMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    source = MaterialParameterSource.Uniform,
                                    textureWrapValue = TextureWrap.Clamp,
                                });
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Texture,
                                source = MaterialParameterSource.Uniform,
                                textureValue = texturePath,
                            });
                        }

                        AddTexture("ambientTexture", string.IsNullOrEmpty(m.AmbientMap?.FileName) == false, m.AmbientMap?.FileName);

                        AddTexture("diffuseTexture", string.IsNullOrEmpty(m.DiffuseMap?.FileName) == false, m.DiffuseMap?.FileName);

                        AddTexture("emissiveTexture", string.IsNullOrEmpty(m.EmissiveMap?.FileName) == false, m.EmissiveMap?.FileName);

                        AddTexture("specularTexture", string.IsNullOrEmpty(m.SpecularMap?.FileName) == false, m.SpecularMap?.FileName);

                        MeshImporterContext.FillMaterialParameters(materialMetadata, standardShader);

                        try
                        {
                            var json = JsonConvert.SerializeObject(materialMetadata, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText(target, json);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            var assetHolder = new AssetHolder()
                            {
                                guid = materialGuid,
                                typeName = typeof(Material).FullName,
                            };

                            var json = JsonConvert.SerializeObject(assetHolder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText($"{target}.meta", json);
                        }
                        catch (Exception)
                        {
                        }

                        Console.WriteLine($"\t\tGenerated material {target}");
                    }
                }
            }
        }
        #endregion

        #region Meshes

        bool TryGetVertex(int index, out Vector3Holder position, out Vector4Holder color)
        {
            if (index == 0)
            {
                position = default;
                color = default;

                return false;
            }

            if (index < 0)
            {
                index = model.Vertices.Count + index;
            }
            else
            {
                index--;
            }

            if (index >= 0 && index < model.Vertices.Count)
            {
                var outValue = model.Vertices[index];

                position = new(new Vector3(outValue.Position.X, outValue.Position.Y, outValue.Position.Z));

                color = outValue.Color.HasValue ? new(new Vector4(outValue.Color.Value.X,
                    outValue.Color.Value.Y,
                    outValue.Color.Value.Z,
                    outValue.Color.Value.W)) : null;

                return true;
            }

            position = default;
            color = default;

            return false;
        }

        bool TryGetTexture(int index, out Vector2Holder uv)
        {
            if (index == 0)
            {
                uv = default;

                return false;
            }

            if (index < 0)
            {
                index = model.TextureVertices.Count + index;
            }
            else
            {
                index--;
            }

            if (index >= 0 && index < model.TextureVertices.Count)
            {
                var outValue = model.TextureVertices[index];

                uv = new(new Vector2(outValue.X, outValue.Y));

                return true;
            }

            uv = default;

            return false;
        }

        bool TryGetNormal(int index, out Vector3Holder normal)
        {
            if (index == 0)
            {
                normal = default;

                return false;
            }

            if (index < 0)
            {
                index = model.VertexNormals.Count + index;
            }
            else
            {
                index--;
            }

            if (index >= 0 && index < model.VertexNormals.Count)
            {
                var outValue = model.VertexNormals[index];

                normal = new(new Vector3(outValue.X, outValue.Y, outValue.Z));

                return true;
            }

            normal = default;

            return false;
        }

        void FillMeshData(MeshAssetMeshInfo mesh, Span<ObjFace> faces)
        {
            foreach (var face in faces)
            {
                (int, int, int)[] indices = [];

                if (face.Vertices.Count == 3)
                {
                    if (metadata.flipWindingOrder)
                    {
                        indices =
                            [
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),
                                (face.Vertices[1].Vertex, face.Vertices[1].Normal, face.Vertices[1].Texture),
                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),
                            ];
                    }
                    else
                    {
                        indices =
                            [
                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),
                                (face.Vertices[1].Vertex, face.Vertices[1].Normal, face.Vertices[1].Texture),
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),
                            ];
                    }

                }
                else if (face.Vertices.Count == 4)
                {
                    if (metadata.flipWindingOrder)
                    {
                        indices =
                            [
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),
                                (face.Vertices[1].Vertex, face.Vertices[1].Normal, face.Vertices[1].Texture),
                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),

                                (face.Vertices[3].Vertex, face.Vertices[3].Normal, face.Vertices[3].Texture),
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),
                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),
                            ];
                    }
                    else
                    {
                        indices =
                            [
                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),
                                (face.Vertices[1].Vertex, face.Vertices[1].Normal, face.Vertices[1].Texture),
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),

                                (face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture),
                                (face.Vertices[2].Vertex, face.Vertices[2].Normal, face.Vertices[2].Texture),
                                (face.Vertices[3].Vertex, face.Vertices[3].Normal, face.Vertices[3].Texture),
                            ];
                    }
                }
                else if (face.Vertices.Count > 4)
                {
                    var outValue = new List<(int, int, int)>();

                    for (var i = 1; i < face.Vertices.Count - 1; i++)
                    {
                        if (metadata.flipWindingOrder)
                        {
                            outValue.Add((face.Vertices[i + 1].Vertex, face.Vertices[i + 1].Normal, face.Vertices[i + 1].Texture));

                            outValue.Add((face.Vertices[i].Vertex, face.Vertices[i].Normal, face.Vertices[i].Texture));

                            outValue.Add((face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture));
                        }
                        else
                        {
                            outValue.Add((face.Vertices[0].Vertex, face.Vertices[0].Normal, face.Vertices[0].Texture));

                            outValue.Add((face.Vertices[i].Vertex, face.Vertices[i].Normal, face.Vertices[i].Texture));

                            outValue.Add((face.Vertices[i + 1].Vertex, face.Vertices[i + 1].Normal, face.Vertices[i + 1].Texture));
                        }
                    }

                    indices = outValue.ToArray();
                }

                foreach (var pair in indices)
                {
                    if (TryGetVertex(pair.Item1, out var position, out var color))
                    {
                        mesh.vertices.Add(position);

                        if (color != null)
                        {
                            mesh.colors.Add(color);
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (TryGetNormal(pair.Item2, out var normal))
                    {
                        mesh.normals.Add(normal);
                    }

                    if (TryGetTexture(pair.Item3, out var texture))
                    {
                        mesh.UV1.Add(texture);
                    }

                    mesh.indices.Add(mesh.vertices.Count - 1);
                }
            }
        }

        if (model.Groups.Count == 0)
        {
            //Material -> Object -> Mesh
            var meshes = new Dictionary<string, Dictionary<string, MeshAssetMeshInfo>>();

            foreach(var face in model.Faces)
            {
                var materialIndex = materialNames.FindIndex(x => x == face.MaterialName);

                if(materialIndex < 0)
                {
                    continue;
                }

                if(!meshes.TryGetValue(face.MaterialName, out var container))
                {
                    container = [];

                    meshes.Add(face.MaterialName, container);
                }

                if(!container.TryGetValue(face.ObjectName, out var mesh))
                {
                    mesh = new MeshAssetMeshInfo()
                    {
                        name = face.ObjectName,
                        topology = MeshTopology.Triangles,
                        type = MeshAssetType.Normal,
                        materialGuid = materialIndex >= 0 && materialIndex < materialMapping.Count ? materialMapping[materialIndex] :
                            AssetDatabase.GetAssetGuid(AssetSerialization.StandardMaterialPath),
                    };

                    container.Add(face.ObjectName, mesh);
                }

                FillMeshData(mesh, [face]);
            }

            foreach(var pair in meshes)
            {
                foreach(var meshPair in pair.Value)
                {
                    var mesh = meshPair.Value;

                    var v = mesh.vertices.Select(x => x.ToVector3()).ToArray();

                    var aabb = AABB.CreateFromPoints(v);

                    mesh.boundsCenter = new(aabb.center);
                    mesh.boundsExtents = new(aabb.size);

                    meshData.meshes.Add(mesh);

                    node.meshIndices.Add(meshData.meshes.Count - 1);
                }
            }
        }
        else
        {
            foreach (var group in model.Groups)
            {
                if (group.Faces.Count == 0)
                {
                    continue;
                }

                var materialIndex = materialNames.FindIndex(x => x == group.Faces[0].MaterialName);

                var newMesh = new MeshAssetMeshInfo()
                {
                    name = group.Name,
                    topology = MeshTopology.Triangles,
                    type = MeshAssetType.Normal,
                    materialGuid = materialIndex >= 0 && materialIndex < materialMapping.Count ? materialMapping[materialIndex] :
                        AssetDatabase.GetAssetGuid(AssetSerialization.StandardMaterialPath),
                };

                FillMeshData(newMesh, CollectionsMarshal.AsSpan(group.Faces));

                var v = newMesh.vertices.Select(x => x.ToVector3()).ToArray();

                var aabb = AABB.CreateFromPoints(v);

                newMesh.boundsCenter = new(aabb.center);
                newMesh.boundsExtents = new(aabb.size);

                meshData.meshes.Add(newMesh);

                node.meshIndices.Add(meshData.meshes.Count - 1);
            }
        }
        #endregion

        return meshData;
    }
}
