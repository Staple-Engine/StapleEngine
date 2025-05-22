using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ufbx;

namespace Baker;

public partial class Program
{
    public static unsafe SerializableMeshAsset ProcessUFBXMesh(MeshAssetMetadata metadata, string meshFileName, string inputPath,
        SerializableShader standardShader, Func<string, bool> ShaderHasParameter)
    {
        /*
        if (metadata.flipUVs)
        {
            flags |= Silk.NET.Assimp.PostProcessSteps.FlipUVs;
        }

        if (metadata.flipWindingOrder)
        {
            flags |= Silk.NET.Assimp.PostProcessSteps.FlipWindingOrder;
        }
        */

        var error = new UfbxError();

        var scene = ufbx.ufbx.UfbxLoadFile(meshFileName, null, error);

        if(scene == null)
        {
            Console.WriteLine($"\t\tFailed to load {Path.GetFileName(meshFileName)}: {error.Description}");

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

        ufbx.UfbxNode FindNode(ufbx.UfbxNode node, string name)
        {
            if (node.Name.Data == name)
            {
                return node;
            }

            var children = node.Children;

            for(ulong i = 0; i < children.Count; i++)
            {
                var target = FindNode(children[i], name);

                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        #region Materials
        var materialMapping = new List<string>();
        var materialEmbeddedTextures = new Dictionary<string, string>();

        lock (meshMaterialLock)
        {
            var counter = 0;

            var materials = scene.Materials;

            for(ulong i = 0; i < materials.Count; i++)
            {
                var material = materials[i];

                var baseName = material.Name.Data.Length > 0 ? material.Name.Data : (++counter).ToString();

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

                if (material.Features.DoubleSided.Enabled)
                {
                    materialMetadata.cullingMode = CullingMode.None;
                }

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

                var pieces = new Dictionary<string, UfbxMaterialMap>()
                {
                    { "ambientColor", material.Fbx.AmbientColor },
                    { "diffuseColor", material.Fbx.DiffuseColor },
                    { "emissiveColor", material.Fbx.EmissionColor },
                    { "reflectiveColor", material.Fbx.ReflectionColor },
                    { "specularColor", material.Fbx.SpecularColor },
                    { "transparentColor", material.Fbx.TransparencyColor },
                };

                foreach (var pair in pieces)
                {
                    if (pair.Value.HasValue)
                    {
                        var v = pair.Value.ValueVec4;

                        AddColor(pair.Key, true, new Color(v.X, v.Y, v.Z, v.W));
                    }
                }

                var textures = new Dictionary<string, UfbxMaterialMap>()
                {
                    { "ambientTexture", material.Fbx.AmbientColor },
                    { "diffuseTexture", material.Fbx.DiffuseColor },
                    { "displacementTexture", material.Fbx.Displacement },
                    { "emissiveTexture", material.Fbx.EmissionColor },
                    { "normalTexture", material.Fbx.NormalMap },
                    { "opacityTexture", material.Fbx.TransparencyColor },
                    { "reflectionTexture", material.Fbx.ReflectionColor },
                    { "specularTexture", material.Fbx.SpecularColor },
                };

                void AddTexture(string name, bool has, UfbxTexture slot)
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
                        var path = slot.Filename.Data;

                        /*
                        if (path.StartsWith('*'))
                        {
                            if (int.TryParse(path.AsSpan(1), out var textureIndex) == false)
                            {
                                return;
                            }

                            var texture = scene->MTextures[textureIndex];

                            if (texture != null)
                            {
                                byte[] textureData = [];

                                var guid = GuidGenerator.Generate().ToString();

                                var innerFileName = texture->MFilename.AsString;

                                var extension = "png";

                                if (texture->MHeight == 0) //Compressed
                                {
                                    var length = 0;

                                    for (var j = 0; j < 9; j++)
                                    {
                                        if (texture->AchFormatHint[j] == 0)
                                        {
                                            length = j;

                                            break;
                                        }
                                    }

                                    extension = Encoding.UTF8.GetString(new Span<byte>(texture->AchFormatHint, length));

                                    textureData = new Span<byte>(texture->PcData, (int)texture->MWidth).ToArray();
                                }
                                else //Uncompressed, recompress to PNG
                                {
                                    textureData = new Span<byte>(texture->PcData, (int)(texture->MWidth * texture->MHeight * 4)).ToArray();

                                    var rawData = new RawTextureData()
                                    {
                                        colorComponents = StandardTextureColorComponents.RGBA,
                                        data = textureData,
                                        width = (int)texture->MWidth,
                                        height = (int)texture->MHeight,
                                    };

                                    textureData = rawData.EncodePNG();
                                }

                                if ((innerFileName?.Length ?? 0) > 0)
                                {
                                    texturePath = $"{innerFileName}.{extension}";
                                }
                                else if (materialEmbeddedTextures.TryGetValue(path, out texturePath) == false)
                                {
                                    texturePath = $"{guid}.{extension}";

                                    materialEmbeddedTextures.AddOrSetKey(path, texturePath);
                                }

                                try
                                {
                                    var t = Path.Combine(Path.GetDirectoryName(meshFileName), texturePath);

                                    if (File.Exists(t) == false)
                                    {
                                        File.WriteAllBytes(t, textureData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                try
                                {
                                    var t = Path.Combine(Path.GetDirectoryName(meshFileName), $"{texturePath}.meta");

                                    if (File.Exists(t) == false)
                                    {
                                        var metadata = new TextureMetadata()
                                        {
                                            guid = guid,
                                        };

                                        var json = JsonConvert.SerializeObject(metadata, Formatting.Indented,
                                            Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText(t, json);
                                    }
                                    else
                                    {
                                        guid = FindGuid<Texture>(t);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                texturePath = guid;
                            }
                        }
                        else
                        */
                        {
                            var pieces = path.Replace("\\", "/").Split("/").ToList();

                            texturePath = path;

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

                            //Console.WriteLine($"\t\tSet Texture {name} to {texturePath}");
                        }
                    }

                    if (texturePath.Length > 0)
                    {
                        mappingU = slot.WrapU switch
                        {
                            UfbxWrapMode.UFBX_WRAP_CLAMP => TextureWrap.Clamp,
                            UfbxWrapMode.UFBX_WRAP_REPEAT => TextureWrap.Repeat,
                            _ => TextureWrap.Clamp,
                        };

                        mappingV = slot.WrapV switch
                        {
                            UfbxWrapMode.UFBX_WRAP_CLAMP => TextureWrap.Clamp,
                            UfbxWrapMode.UFBX_WRAP_REPEAT => TextureWrap.Repeat,
                            _ => TextureWrap.Clamp,
                        };
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

                foreach (var pair in textures)
                {
                    if (pair.Value.TextureEnabled)
                    {
                        AddTexture(pair.Key, true, pair.Value.Texture);
                    }
                }

                //TODO
                /*
                if (material.IsPBRMaterial)
                {
                    AddTexture("baseColorTexture", material.PBR.HasTextureBaseColor, material.PBR.TextureBaseColor);
                    AddTexture("roughnessTexture", material.PBR.HasTextureRoughness, material.PBR.TextureRoughness);
                    AddTexture("metalnessTexture", material.PBR.HasTextureMetalness, material.PBR.TextureMetalness);
                    AddTexture("normalCameraTexture", material.PBR.HasTextureNormalCamera, material.PBR.TextureNormalCamera);
                    AddTexture("emissionColorTexture", material.PBR.HasTextureEmissionColor, material.PBR.TextureEmissionColor);
                }
                */

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
            return value;
        }

        var nodes = new List<MeshAssetNode>();

        var nodeToName = new Dictionary<UfbxNode, string>();
        var nodeToIndex = new Dictionary<UfbxNode, int>();
        var localNodes = new Dictionary<int, UfbxNode>();
        var nodeCounters = new Dictionary<string, int>();

        void RegisterNode(UfbxNode node, MeshAssetNode parent)
        {
            var nodeName = node.Name.Data;

            if (nodeCounters.TryGetValue(nodeName, out var counter) == false)
            {
                counter = 0;

                nodeCounters.Add(nodeName, counter);
            }

            nodeName = counter == 0 ? nodeName : $"{nodeName}{counter}";

            nodeCounters[nodeName] = counter + 1;

            var translation = node.LocalTransform.Translation.ToVector3();
            var rotation = node.LocalTransform.Rotation.ToQuaternion();
            var scale = node.LocalTransform.Scale.ToVector3();

            var meshIndices = new List<int>();

            if(node.Mesh != null)
            {
                var index = -1;

                for(ulong i = 0; i < scene.Meshes.Count; i++)
                {
                    if (scene.Meshes[i] == node.Mesh)
                    {
                        index = (int)i;

                        break;
                    }
                }

                if(index >= 0)
                {
                    meshIndices.Add(index);
                }
            }

            var newNode = new MeshAssetNode()
            {
                name = nodeName,
                meshIndices = meshIndices,
                position = new Vector3Holder(translation),
                scale = new Vector3Holder(scale),
                rotation = new Vector3Holder(rotation),
            };

            var currentIndex = nodes.Count;

            nodeToName.Add(node, nodeName);
            nodeToIndex.Add(node, currentIndex);
            localNodes.Add(currentIndex, node);

            parent?.children.Add(nodes.Count);

            nodes.Add(newNode);

            var children = node.Children;

            for(ulong i = 0; i < children.Count; i++)
            {
                RegisterNode(children[i], newNode);
            }
        }

        MeshAssetNode rootNode = null;

        if (metadata.scale != 1 || metadata.convertUnits || metadata.rotation != MeshAssetRotation.None)
        {
            var rotation = metadata.rotation switch
            {
                MeshAssetRotation.NinetyPositive => Quaternion.CreateFromAxisAngle(new(1, 0, 0), 90 * Staple.Math.Deg2Rad),
                MeshAssetRotation.NinetyNegative => Quaternion.CreateFromAxisAngle(new(1, 0, 0), -90 * Staple.Math.Deg2Rad),
                _ => Quaternion.Identity,
            };

            var scale = Vector3.One * metadata.scale * (metadata.convertUnits ? 0.01f : 1.0f);

            rootNode = new()
            {
                name = "StapleRoot",
                position = new(),
                rotation = new(rotation),
                scale = new(scale),
            };

            nodes.Add(rootNode);
        }

        RegisterNode(scene.RootNode, null);

        meshData.nodes = nodes.ToArray();

        #region Meshes
        var meshes = scene.Meshes;

        for(ulong i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];

            var materialIndex = -1;

            if(mesh.Materials.Count > 0)
            {
                for (ulong j = 0; j < scene.Materials.Count; j++)
                {
                    if(scene.Materials[j] == mesh.Materials[0])
                    {
                        materialIndex = (int)j;

                        break;
                    }
                }
            }

            var m = new MeshAssetMeshInfo
            {
                name = mesh.Name.Data,
                materialGuid = materialIndex >= 0 ? materialMapping[materialIndex] : "",
                type = mesh.SkinDeformers.Count > 0 ? MeshAssetType.Skinned : MeshAssetType.Normal,
            };

            {
                var aabb = AABB.CreateFromPoints(mesh.Vertices.ToVector3Array());

                var center = aabb.center;
                var size = aabb.size;

                m.boundsCenter = new Vector3Holder(new Vector3(center.X, center.Y, center.Z));
                m.boundsExtents = new Vector3Holder(new Vector3(size.X, size.Y, size.Z));
            }

            if(mesh.NumTriangles == 0)
            {
                Console.WriteLine($"\t\tWARNING: Mesh {m.name} of {Path.GetFileNameWithoutExtension(meshFileName)} isn't composed of only triangles, adding as empty mesh...");

                meshData.meshes.Add(m);

                continue;
            }

            m.topology = MeshTopology.Triangles;

            var vertexCount = mesh.NumVertices;

            var vertices = new List<Vector3Holder>();
            var tangents = new List<Vector3Holder>();
            var bitangents = new List<Vector3Holder>();
            var indices = new List<int>();
            var normals = new Vector3[vertexCount];

            for (ulong j = 0; j < vertexCount; j++)
            {
                vertices.Add(ApplyTransform(new Vector3Holder(mesh.Vertices[j].ToVector3())));

                if (mesh.VertexTangent.Exists)
                {
                    tangents.Add(ApplyNormalTransform(new Vector3Holder(mesh.VertexTangent[j].ToVector3())));
                }

                if (mesh.VertexBitangent.Exists)
                {
                    bitangents.Add(ApplyNormalTransform(new Vector3Holder(mesh.VertexBitangent[j].ToVector3())));
                }

                if (mesh.VertexNormal.Exists)
                {
                    normals[j] = mesh.VertexNormal[j].ToVector3();
                }
            }

            var faces = mesh.Faces;

            for(ulong j = 0; j < faces.Count; j++)
            {
                var face = faces[j];

                for(var k = face.IndexBegin; k < face.IndexBegin + face.NumIndices; k++)
                {
                    m.indices.Add((int)mesh.VertexIndices[k]);
                }
            }

            for (ulong j = 0; j < 4; j++)
            {
                if (j >= mesh.ColorSets.Count)
                {
                    break;
                }

                var set = mesh.ColorSets[j];

                if(set.VertexColor.Exists == false)
                {
                    continue;
                }

                var colors = new Vector4Holder[set.VertexColor.Indices.Count];

                for(var k = 0; k < colors.Length; k++)
                {
                    var c = mesh.VertexColor[set.VertexColor.Indices[j]];

                    colors[k] = new(new Vector4(c.X, c.Y, c.Z, c.W));
                }

                switch (j)
                {
                    case 0:

                        m.colors.AddRange(colors);

                        break;

                    case 1:

                        m.colors2.AddRange(colors);

                        break;

                    case 2:

                        m.colors3.AddRange(colors);

                        break;

                    case 3:

                        m.colors4.AddRange(colors);

                        break;
                }
            }

            m.vertices = vertices;

            m.tangents = tangents;

            m.bitangents = bitangents;

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

            for (ulong j = 0; j < 8; j++)
            {
                if(j >= mesh.UvSets.Count)
                {
                    continue;
                }

                var set = mesh.UvSets[j];

                if(set.VertexUv.Exists == false)
                {
                    continue;
                }

                var uv = new Vector2Holder[set.VertexUv.Indices.Count];

                for(ulong k = 0; k < set.VertexUv.Indices.Count; k++)
                {
                    var v = set.VertexUv.Values[set.VertexUv.Indices[k]];

                    uv[k] = new Vector2Holder(new Vector2(v.X, v.Y));
                }

                uvs[j].AddRange(uv);
            }

            if (mesh.SkinDeformers.Count > 0)
            {
                var boneIndices = new List<Vector4Filler>();
                var boneWeights = new List<Vector4Filler>();

                for (ulong j = 0; j < mesh.NumVertices; j++)
                {
                    boneIndices.Add(new());
                    boneWeights.Add(new());
                }

                var bones = mesh.SkinDeformers;

                var invalidBones = new HashSet<string>();

                for (ulong j = 0; j < bones.Count; j++)
                {
                    var bone = bones[j];

                    Matrix4x4.Decompose(Matrix4x4.Transpose(bone->MOffsetMatrix), out var scale, out var rotation, out var translation);

                    var boneNode = FindNode(scene->MRootNode, bone->MName.AsString);

                    var validBone = boneNode != null;

                    var nodeIndex = -1;

                    if (nodeToIndex.TryGetValue((nint)boneNode, out nodeIndex) == false)
                    {
                        invalidBones.Add(bone->MName.AsString);

                        validBone = false;

                        nodeIndex = -1;
                    }

                    m.bones.Add(new()
                    {
                        nodeIndex = nodeIndex,
                        offsetPosition = new(translation),
                        offsetScale = new(scale),
                        offsetRotation = new(rotation),
                    });

                    var weights = bone->GetWeights();

                    for (var k = 0; k < weights.Length; k++)
                    {
                        var item = weights[k];

                        var boneIndex = boneIndices[(int)item.MVertexId];
                        var boneWeight = boneWeights[(int)item.MVertexId];

                        boneIndex.Add(j);
                        boneWeight.Add(validBone ? item.MWeight : 0);
                    }
                }

                m.boneIndices = boneIndices
                    .Select(x => x.ToHolder())
                    .ToList();

                m.boneWeights = boneWeights
                    .Select(x => x.ToHolderNormalized())
                    .ToList();

                if (invalidBones.Count > 0)
                {
                    Console.WriteLine($"\t\t\tWARNING: {mesh->MName.AsString} of {Path.GetFileName(meshFileName)} has an invalid bones: " +
                        $"{string.Join(", ", invalidBones)}");
                }
            }

            meshData.meshes.Add(m);
        }
        #endregion

        #region Animations
        var animations = scene->Animations();
        var animationCounter = 0;

        for (var j = 0; j < animations.Length; j++)
        {
            var animation = animations[j];

            var a = new MeshAssetAnimation()
            {
                duration = (float)(animation->MDuration / animation->MTicksPerSecond),
                name = animation->MName.AsString ?? $"Unnamed {++animationCounter}",
            };

            var channels = animation->Channels();

            foreach (var channel in channels)
            {
                var positionKeys = channel->PositionKeys()
                    .Select(x => new MeshAssetVectorAnimationKey()
                    {
                        time = (float)(x.MTime / animation->MTicksPerSecond),
                        value = new(x.MValue),
                    })
                    .ToList();

                var rotationKeys = channel->RotationKeys()
                    .Select(x => new MeshAssetQuaternionAnimationKey()
                    {
                        time = (float)(x.MTime / animation->MTicksPerSecond),
                        value = new(x.MValue),
                    })
                    .ToList();

                var scaleKeys = channel->ScaleKeys()
                    .Select(x => new MeshAssetVectorAnimationKey()
                    {
                        time = (float)(x.MTime / animation->MTicksPerSecond),
                        value = new(x.MValue),
                    })
                    .ToList();

                var node = FindNode(scene->MRootNode, channel->MNodeName.AsString);

                var c = new MeshAssetAnimationChannel()
                {
                    nodeIndex = nodeToIndex[(nint)node],
                    positionKeys = positionKeys,
                    rotationKeys = rotationKeys,
                    scaleKeys = scaleKeys,
                };

                a.channels.Add(c);
            }

            meshData.animations.Add(a);
        }
        #endregion

        return meshData;
        */
    }
}
