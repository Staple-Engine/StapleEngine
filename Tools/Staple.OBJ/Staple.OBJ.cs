using JeremyAnsel.Media.WavefrontObj;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Tooling;

public class OBJImporter : IMeshImporter
{
    public bool HandlesExtension(string extension) => extension == ".obj";

    public SerializableMeshAsset ImportMesh(MeshImporterContext context)
    {
        var (metadata, meshFileName, inputPath, standardShader, ShaderHasParameter, meshMaterialLock, processedTextures) =
            (context.metadata, context.meshFileName, context.inputPath, context.standardShader,
            context.ShaderHasParameter, context.materialLock, context.processedTextures);

        ObjFile model = null;
        ObjMaterialFile materials = null;

        try
        {
            model = ObjFile.FromFile(meshFileName);
        }
        catch(Exception e)
        {
            Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}: {e}");

            return null;
        }

        //Materials are optional
        try
        {
            materials = ObjMaterialFile.FromFile($"{Path.GetDirectoryName(meshFileName)}/{Path.GetFileNameWithoutExtension(meshFileName)}.mtl");
        }
        catch(Exception)
        {
        }

        if (model == null)
        {
            Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}");

            return null;
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

        if (materials != null)
        {
            lock (meshMaterialLock)
            {
                var counter = 0;

                foreach (var material in materials.Materials)
                {
                    var baseName = string.IsNullOrEmpty(material.Name) == false ? material.Name : (++counter).ToString();

                    baseName = string.Join('_', baseName.Split(Path.GetInvalidFileNameChars()));

                    var fileName = $"{baseName}.{AssetSerialization.MaterialExtension}";

                    var target = Path.Combine(Path.GetDirectoryName(meshFileName), fileName);
                    var materialGuid = Utilities.FindGuid<Material>($"{target}.meta");

                    materialMapping.Add(materialGuid);

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

                    AddColor("ambientColor", material.AmbientColor != null, material.AmbientColor);
                    AddColor("diffuseColor", material.DiffuseColor != null, material.DiffuseColor);
                    AddColor("emissiveColor", material.EmissiveColor != null, material.EmissiveColor);
                    AddColor("specularColor", material.SpecularColor != null, material.SpecularColor);

                    void AddTexture(string name, bool has, string fileName)
                    {
                        if (ShaderHasParameter(name) == false)
                        {
                            return;
                        }

                        var texturePath = "";

                        if (has)
                        {
                            var path = fileName;

                            if (Path.IsPathRooted(path) == false)
                            {
                                var localPath = Path.GetDirectoryName(meshFileName);

                                path = Path.GetFullPath(path, Path.GetDirectoryName(meshFileName));

                                var index = path.IndexOf(localPath);

                                if (index >= 0)
                                {
                                    path = path.Substring(index + localPath.Length + 1);
                                }

                                path = path.Replace(Path.PathSeparator, '/');
                            }

                            var pieces = path.Replace("\\", "/").Split("/").ToList();

                            texturePath = path;

                            var initialPath = Path.Combine(Path.GetDirectoryName(meshFileName), path);

                            var ok = false;

                            if (File.Exists(initialPath))
                            {
                                if (processedTextures.TryGetValue($"{initialPath}.meta", out var guid))
                                {
                                    texturePath = guid;

                                    ok = true;
                                }
                            }

                            if (ok == false)
                            {
                                while (pieces.Count > 0)
                                {
                                    try
                                    {
                                        var baseP = Path.Combine(Path.GetDirectoryName(meshFileName), string.Join("/", pieces.Take(pieces.Count - 1)));

                                        var directories = Directory.GetDirectories(baseP);

                                        bool Find(string path)
                                        {
                                            var p = Path.Combine(path, pieces.Last()).Replace("\\", "/");

                                            if (File.Exists(p))
                                            {
                                                texturePath = string.Join("/", pieces);

                                                if (processedTextures.TryGetValue($"{p}.meta", out var guid))
                                                {
                                                    texturePath = guid;
                                                }
                                                else
                                                {
                                                    Console.WriteLine($"\t\tUnable to find local texture guid for {p}");

                                                    texturePath = "";
                                                }

                                                return true;
                                            }

                                            return false;
                                        }

                                        var found = false;

                                        foreach (var directory in directories)
                                        {
                                            found = Find(Path.Combine(baseP, directory));

                                            if (found)
                                            {
                                                break;
                                            }
                                        }

                                        if (found)
                                        {
                                            break;
                                        }

                                        found = Find(baseP);

                                        if (found)
                                        {
                                            break;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    pieces.RemoveAt(0);
                                }

                                if (pieces.Count == 0)
                                {
                                    Console.WriteLine($"\t\tUnable to find local texture path for {path}");

                                    texturePath = "";
                                }
                            }

                            //Console.WriteLine($"\t\tSet Texture {name} to {texturePath}");
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

                    AddTexture("ambientTexture", string.IsNullOrEmpty(material.AmbientMap?.FileName) == false, material.AmbientMap?.FileName);

                    AddTexture("diffuseTexture", string.IsNullOrEmpty(material.DiffuseMap?.FileName) == false, material.DiffuseMap?.FileName);

                    AddTexture("emissiveTexture", string.IsNullOrEmpty(material.EmissiveMap?.FileName) == false, material.EmissiveMap?.FileName);

                    AddTexture("specularTexture", string.IsNullOrEmpty(material.SpecularMap?.FileName) == false, material.SpecularMap?.FileName);

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
        #endregion

        #region Meshes
        foreach (var group in model.Groups)
        {
            if(group.Faces.Count == 0)
            {
                continue;
            }

            var materialIndex = materials?.Materials?.FindIndex( x => x.Name == group.Faces[0].MaterialName) ?? -1;

            var newMesh = new MeshAssetMeshInfo()
            {
                name = group.Name,
                topology = MeshTopology.Triangles,
                type = MeshAssetType.Normal,
                materialGuid = materialIndex >= 0 && materialIndex < materialMapping.Count ? materialMapping[materialIndex] :
                    AssetDatabase.GetAssetGuid(AssetSerialization.StandardMaterialPath),
            };

            bool TryGetVertex(int index, out Vector3Holder position, out Vector4Holder color)
            {
                if(index == 0)
                {
                    position = default;
                    color = default;

                    return false;
                }

                if(index < 0)
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
                    index = model.Vertices.Count + index;
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
                    index = model.Vertices.Count + index;
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

            foreach (var face in group.Faces)
            {
                (int, int, int)[] indices = [];

                if (face.Vertices.Count == 3)
                {
                    if(metadata.flipWindingOrder)
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
                else if(face.Vertices.Count == 4)
                {
                    if(metadata.flipWindingOrder)
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
                else if(face.Vertices.Count > 4)
                {
                    var outValue = new List<(int, int, int)>();

                    for(var i = 1; i < face.Vertices.Count - 1; i++)
                    {
                        if(metadata.flipWindingOrder)
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

                foreach(var pair in indices)
                {
                    if(TryGetVertex(pair.Item1, out var position, out var color))
                    {
                        newMesh.vertices.Add(position);
                        
                        if(color != null)
                        {
                            newMesh.colors.Add(color);
                        }
                    }
                    else
                    {
                        continue;
                    }

                    if (TryGetNormal(pair.Item2, out var normal))
                    {
                        newMesh.normals.Add(normal);
                    }

                    if(TryGetTexture(pair.Item3, out var texture))
                    {
                        newMesh.UV1.Add(texture);
                    }

                    newMesh.indices.Add(newMesh.vertices.Count - 1);
                }
            }

            var v = newMesh.vertices.Select(x => x.ToVector3()).ToArray();

            var aabb = AABB.CreateFromPoints(v);

            newMesh.boundsCenter = new(aabb.center);
            newMesh.boundsExtents = new(aabb.size);

            meshData.meshes.Add(newMesh);

            node.meshIndices.Add(meshData.meshes.Count - 1);
        }
        #endregion

        return meshData;
    }
}
