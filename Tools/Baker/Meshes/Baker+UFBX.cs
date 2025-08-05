using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using UFBX;

namespace Baker;

public partial class Program
{
    public static unsafe SerializableMeshAsset ProcessUFBXMesh(MeshAssetMetadata metadata, string meshFileName, string inputPath,
        SerializableShader standardShader, Func<string, bool> ShaderHasParameter)
    {
        var scene = UFBX.UFBX.LoadScene(meshFileName);

        if(scene == null)
        {
            Console.WriteLine($"\t\tError: Failed to import file {meshFileName}");

            return null;
        }

        if (metadata.frameRate <= 0)
        {
            metadata.frameRate = 30;
        }

        var meshData = new SerializableMeshAsset
        {
            metadata = metadata,
        };

        #region Materials
        var materialMapping = new List<string>();
        var materialEmbeddedTextures = new Dictionary<string, string>();

        lock (meshMaterialLock)
        {
            var counter = 0;

            var materials = scene->Materials;

            foreach (var material in materials)
            {
                var baseName = material.name.length > 0 ? material.name.Value : (++counter).ToString();

                baseName = string.Join('_', baseName.Split(Path.GetInvalidFileNameChars()));

                var fileName = $"{baseName}.{AssetSerialization.MaterialExtension}";

                var target = Path.Combine(Path.GetDirectoryName(meshFileName), fileName);
                var materialGuid = FindGuid<Material>($"{target}.meta");

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
                    shader = AssetSerialization.StandardShaderGUID,
                };

                //TODO: TwoSided

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

                void AddColor(string name, bool has, Vector4 color)
                {
                    if (ShaderHasParameter(name) == false)
                    {
                        return;
                    }

                    var c = Color.White;

                    if (has)
                    {
                        c.r = color.X;
                        c.g = color.Y;
                        c.b = color.Z;
                        c.a = color.W;
                    }

                    materialMetadata.parameters.Add(name, new MaterialParameter()
                    {
                        type = MaterialParameterType.Color,
                        source = MaterialParameterSource.Uniform,
                        colorValue = c,
                    });
                }

                AddColor("ambientColor", material.ambientColor != Vector4.Zero, material.ambientColor);
                AddColor("diffuseColor", material.diffuseColor != Vector4.Zero, material.diffuseColor);
                AddColor("emissiveColor", material.emissionColor != Vector4.Zero, material.emissionColor);
                AddColor("reflectiveColor", material.reflectionColor != Vector4.Zero, material.reflectionColor);
                AddColor("specularColor", material.specularColor != Vector4.Zero, material.specularColor);
                AddColor("transparentColor", material.transparencyColor != Vector4.Zero, material.transparencyColor);

                void AddTexture(string name, bool has, UFBXString fileName, TextureWrap wrapU, TextureWrap wrapV)
                {
                    if (ShaderHasParameter(name) == false)
                    {
                        return;
                    }

                    var texturePath = "";

                    var mappingU = TextureWrap.Clamp;
                    var mappingV = TextureWrap.Clamp;

                    if (has)
                    {
                        var path = fileName.Value;

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

                    if (texturePath.Length > 0)
                    {
                        mappingU = wrapU;
                        mappingV = wrapV;
                    }

                    if (ShaderHasParameter($"{name}_UMapping") && ShaderHasParameter($"{name}_VMapping"))
                    {
                        materialMetadata.parameters.Add($"{name}_UMapping", new MaterialParameter()
                        {
                            type = MaterialParameterType.TextureWrap,
                            source = MaterialParameterSource.Uniform,
                            textureWrapValue = mappingU,
                        });

                        materialMetadata.parameters.Add($"{name}_VMapping", new MaterialParameter()
                        {
                            type = MaterialParameterType.TextureWrap,
                            source = MaterialParameterSource.Uniform,
                            textureWrapValue = mappingV,
                        });
                    }

                    materialMetadata.parameters.Add(name, new MaterialParameter()
                    {
                        type = MaterialParameterType.Texture,
                        source = MaterialParameterSource.Uniform,
                        textureValue = texturePath,
                    });
                }

                AddTexture("ambientTexture", material.ambientTexture.length > 0, material.ambientTexture,
                    (TextureWrap)material.ambientWrapU, (TextureWrap)material.ambientWrapV);

                AddTexture("diffuseTexture", material.diffuseTexture.length > 0, material.diffuseTexture,
                    (TextureWrap)material.diffuseWrapU, (TextureWrap)material.diffuseWrapV);

                AddTexture("emissiveTexture", material.emissionTexture.length > 0, material.emissionTexture,
                    (TextureWrap)material.emissionWrapU, (TextureWrap)material.emissionWrapV);

                AddTexture("reflectiveTexture", material.reflectionTexture.length > 0, material.reflectionTexture,
                    (TextureWrap)material.reflectionWrapU, (TextureWrap)material.reflectionWrapV);

                AddTexture("specularTexture", material.specularTexture.length > 0, material.specularTexture,
                    (TextureWrap)material.specularWrapU, (TextureWrap)material.specularWrapV);

                AddTexture("transparentTexture", material.transparencyTexture.length > 0, material.transparencyTexture,
                    (TextureWrap)material.transparencyWrapU, (TextureWrap)material.transparencyWrapV);

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
        #endregion

        Vector3Holder ApplyTransform(Vector3Holder value)
        {
            return value;
        }

        Vector3Holder ApplyNormalTransform(Vector3Holder value)
        {
            /*
            //Must reverse the angle
            var rotation = metadata.rotation switch
            {
                MeshAssetRotation.NinetyPositive => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * -90),
                MeshAssetRotation.NinetyNegative => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad * 90),
                _ => Matrix4x4.Identity,
            };

            return new(Vector3.Normalize(Vector3.TransformNormal(value.ToVector3(), rotation)));
            */

            return value;
        }

        var nodes = new List<MeshAssetNode>();

        var nodeToName = new Dictionary<nint, string>();
        var nodeToIndex = new Dictionary<nint, int>();
        var localNodes = new Dictionary<int, nint>();
        var nodeCounters = new Dictionary<string, int>();

        foreach(var node in scene->Nodes)
        {
            var nodeName = node.name.Value;

            if (nodeCounters.TryGetValue(nodeName, out var counter) == false)
            {
                counter = 0;

                nodeCounters.Add(nodeName, counter);
            }

            nodeName = counter == 0 ? nodeName : $"{nodeName}{counter}";

            nodeCounters[nodeName] = counter + 1;

            var translation = node.localTransform.position;
            var rotation = node.localTransform.rotation;
            var scale = node.localTransform.scale;

            var meshIndices = node.MeshIndices
                .ToArray()
                .ToList();

            var newNode = new MeshAssetNode()
            {
                name = nodeName,
                meshIndices = meshIndices,
                position = new Vector3Holder(translation),
                scale = new Vector3Holder(scale),
                rotation = new Vector3Holder(rotation),
            };

            var parent = node.parentIndex >= 0 ? nodes[node.parentIndex] : null;

            parent?.children.Add(nodes.Count);

            nodes.Add(newNode);
        }

        meshData.nodes = nodes.ToArray();

        #region Meshes
        foreach (var mesh in scene->Meshes)
        {
            var m = new MeshAssetMeshInfo
            {
                name = $"Mesh {meshData.meshes.Count}",
                materialGuid = mesh.materialIndex >= 0 && mesh.materialIndex < materialMapping.Count ? materialMapping[mesh.materialIndex] : "",
                type = mesh.isSkinned ? MeshAssetType.Skinned : MeshAssetType.Normal,
                topology = MeshTopology.Triangles,
            };

            {
                var aabb = AABB.CreateFromPoints(mesh.Vertices);

                m.boundsCenter = new Vector3Holder(aabb.center);
                m.boundsExtents = new Vector3Holder(aabb.size);
            }

            var vertexCount = mesh.vertexCount;

            var vertices = new List<Vector3Holder>();
            var tangents = new List<Vector3Holder>();
            var bitangents = new List<Vector3Holder>();
            var indices = new List<int>();
            var normals = new Vector3[vertexCount];

            for (var j = 0; j < vertexCount; j++)
            {
                vertices.Add(ApplyTransform(new Vector3Holder(mesh.Vertices[j])));

                if (mesh.Tangents.Length > 0)
                {
                    tangents.Add(ApplyNormalTransform(new Vector3Holder(mesh.Tangents[j])));
                }

                if (mesh.Bitangents.Length > 0)
                {
                    bitangents.Add(ApplyNormalTransform(new Vector3Holder(mesh.Bitangents[j])));
                }

                if (mesh.Normals.Length > 0)
                {
                    normals[j] = mesh.Normals[j];
                }
            }

            indices.AddRange(mesh.Indices.ToArray().Select(x => (int)x).ToArray());

            for (var k = 0; k < 4; k++)
            {
                switch (k)
                {
                    case 0:

                        if(mesh.Color0.Length > 0)
                        {
                            m.colors.AddRange(mesh.Color0.ToArray().Select(x => new Vector4Holder(x)));
                        }

                        break;

                    case 1:

                        if (mesh.Color1.Length > 0)
                        {
                            m.colors2.AddRange(mesh.Color1.ToArray().Select(x => new Vector4Holder(x)));
                        }

                        break;

                    case 2:

                        if (mesh.Color2.Length > 0)
                        {
                            m.colors3.AddRange(mesh.Color2.ToArray().Select(x => new Vector4Holder(x)));
                        }

                        break;

                    case 3:

                        if (mesh.Color3.Length > 0)
                        {
                            m.colors4.AddRange(mesh.Color3.ToArray().Select(x => new Vector4Holder(x)));
                        }

                        break;
                }
            }

            m.vertices = vertices;

            m.tangents = tangents;

            m.bitangents = bitangents;

            if (metadata.flipWindingOrder && indices.Count % 3 == 0)
            {
                for (var k = 0; k < indices.Count; k += 3)
                {
                    (indices[k + 1], indices[k + 2]) = (indices[k + 2], indices[k + 1]);
                }
            }

            m.indices = indices;

            if (metadata.regenerateNormals)
            {
                var v = m.vertices
                    .Select(x => x.ToVector3())
                    .ToArray();

                normals = Mesh.GenerateNormals(v, CollectionsMarshal.AsSpan(m.indices), metadata.useSmoothNormals);
            }

            m.normals = normals
                .Select(x => ApplyNormalTransform(new Vector3Holder(x)))
                .ToList();

            var uvs = new List<Vector2Holder>[8]
            {
                m.UV1,
                m.UV2,
                m.UV3,
                m.UV4,
                m.UV5,
                m.UV6,
                m.UV7,
                m.UV8,
            };

            for (var j = 0; j < 8; j++)
            {
                var source = j switch
                {
                    0 => mesh.UV0,
                    1 => mesh.UV1,
                    2 => mesh.UV2,
                    3 => mesh.UV3,
                    4 => mesh.UV4,
                    5 => mesh.UV5,
                    6 => mesh.UV6,
                    7 => mesh.UV7,
                    _ => default,
                };

                if (source.Length > 0)
                {
                    var sourceUVs = source.ToArray();

                    if(metadata.flipUVs)
                    {
                        for(var k = 0; k < sourceUVs.Length; k++)
                        {
                            sourceUVs[k].Y = 1 - sourceUVs[k].Y;
                        }
                    }

                    uvs[j].AddRange(sourceUVs.Select(x => new Vector2Holder(x)));
                }
            }

            if (mesh.isSkinned)
            {
                var bones = mesh.Bones;

                for (var j = 0; j < bones.Length; j++)
                {
                    var bone = bones[j];

                    m.bones.Add(new()
                    {
                        nodeIndex = bone.nodeIndex,
                        offsetMatrix = Matrix4x4Holder.FromMatrix(Matrix4x4.Transpose(bone.offsetMatrix)),
                    });
                }

                m.boneIndices = mesh.BoneIndices
                    .ToArray()
                    .Select(x => new Vector4Holder(x))
                    .ToList();

                m.boneWeights = mesh.BoneWeights
                    .ToArray()
                    .Select(x => new Vector4Holder(x))
                    .ToList();
            }

            meshData.meshes.Add(m);
        }
        #endregion

        #region Animations
        var animations = scene->Animations;

        for (var j = 0; j < animations.Length; j++)
        {
            var animation = animations[j];

            var a = new MeshAssetAnimation()
            {
                duration = animation.duration,
                name = animation.name.Value,
            };

            for(var k = 0; k < animation.nodeCount; k++)
            {
                var frame = animation.nodes[k];

                var positionKeys = frame.Positions
                    .ToArray()
                    .Select(x => new MeshAssetVectorAnimationKey()
                    {
                        time = x.time,
                        value = new(x.value)
                    })
                    .ToList();

                var rotationKeys = frame.Rotations
                    .ToArray()
                    .Select(x => new MeshAssetQuaternionAnimationKey()
                    {
                        time = x.time,
                        value = new(x.value)
                    })
                    .ToList();

                var scaleKeys = frame.Scales
                    .ToArray()
                    .Select(x => new MeshAssetVectorAnimationKey()
                    {
                        time = x.time,
                        value = new(x.value)
                    })
                    .ToList();

                var c = new MeshAssetAnimationChannel()
                {
                    nodeIndex = nodes.FindIndex(x => x.name == scene->nodes[frame.nodeIndex].name.Value),
                    positionKeys = positionKeys,
                    rotationKeys = rotationKeys,
                    scaleKeys = scaleKeys,
                };

                a.channels.Add(c);
            }

            meshData.animations.Add(a);
        }
        #endregion

        UFBX.UFBX.FreeScene(scene);

        return meshData;
    }
}
