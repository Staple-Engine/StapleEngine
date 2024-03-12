using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Baker;

class Vector4Filler
{
    public float x;
    public float y;
    public float z;
    public float w;

    public int index = 0;

    public void Add(float value)
    {
        switch(index++)
        {
            case 0:

                x = value;

                break;

            case 1:

                y = value;
                
                break;

            case 2:

                z = value;

                break;

            case 3:

                w = value;

                break;

            default:

                break;
        }
    }

    public Vector4Holder ToHolder()
    {
        return new()
        {
            x = x,
            y = y,
            z = z,
            w = w,
        };
    }
}

static partial class Program
{
    private static object meshMaterialLock = new();

    private static void ProcessMeshes(AppPlatform platform, string inputPath, string outputPath)
    {
        var meshFiles = new List<string>();

        foreach (var extension in AssetSerialization.MeshExtensions)
        {
            try
            {
                meshFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }
        }

        Console.WriteLine($"Processing {meshFiles.Count} meshes...");

        for (var i = 0; i < meshFiles.Count; i++)
        {
            var meshFileName = meshFiles[i];

            Console.WriteLine($"\t{meshFileName}");

            try
            {
                if (File.Exists(meshFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {meshFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {meshFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Mesh>(meshFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(meshFileName));
            var file = Path.GetFileName(meshFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(meshFileName, outputFile) == false &&
                ShouldProcessFile(meshFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(meshFileName.Replace(".meta", "")), () =>
            {
                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MeshAssetMetadata metadata;

                try
                {
                    text = File.ReadAllText(meshFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MeshAssetMetadata>(text);

                    metadata.guid = guid;
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    return;
                }

                using var context = new Assimp.AssimpContext();

                context.XAxisRotation = metadata.rotation switch
                {
                    MeshAssetRotation.NinetyNegative => -90,
                    MeshAssetRotation.NinetyPositive => 90,
                    _ => 0
                };

                context.Scale = metadata.scale;

                var flags = Assimp.PostProcessSteps.TransformUVCoords |
                    Assimp.PostProcessSteps.GenerateSmoothNormals |
                    Assimp.PostProcessSteps.GenerateUVCoords |
                    Assimp.PostProcessSteps.FindDegenerates |
                    Assimp.PostProcessSteps.FindInvalidData |
                    Assimp.PostProcessSteps.FixInFacingNormals |
                    Assimp.PostProcessSteps.ImproveCacheLocality |
                    Assimp.PostProcessSteps.JoinIdenticalVertices |
                    Assimp.PostProcessSteps.Triangulate |
                    Assimp.PostProcessSteps.SortByPrimitiveType |
                    Assimp.PostProcessSteps.RemoveRedundantMaterials |
                    Assimp.PostProcessSteps.CalculateTangentSpace |
                    Assimp.PostProcessSteps.LimitBoneWeights |
                    Assimp.PostProcessSteps.GenerateBoundingBoxes;

                if (metadata.convertUnits)
                {
                    flags |= Assimp.PostProcessSteps.GlobalScale;
                }

                if (metadata.makeLeftHanded)
                {
                    flags |= Assimp.PostProcessSteps.MakeLeftHanded;
                }

                if (metadata.flipUVs)
                {
                    flags |= Assimp.PostProcessSteps.FlipUVs;
                }

                if (metadata.flipWindingOrder || metadata.rotation != MeshAssetRotation.None)
                {
                    flags |= Assimp.PostProcessSteps.FlipWindingOrder;
                }

                if (metadata.splitLargeMeshes)
                {
                    flags |= Assimp.PostProcessSteps.SplitLargeMeshes;
                }

                if (metadata.preTransformVertices)
                {
                    flags |= Assimp.PostProcessSteps.PreTransformVertices;
                }

                if (metadata.debone)
                {
                    flags |= Assimp.PostProcessSteps.Debone;
                }

                if (metadata.splitByBoneCount)
                {
                    flags |= Assimp.PostProcessSteps.SplitByBoneCount;
                }

                Assimp.Scene scene = null;

                try
                {
                    scene = context.ImportFile(meshFileName.Replace(".meta", ""), flags);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to import file: {e}");

                    return;
                }

                var meshData = new SerializableMeshAsset
                {
                    metadata = metadata,
                };

                Matrix4x4 ToMatrix4x4(Assimp.Matrix4x4 matrix)
                {
                    matrix.Decompose(out var scale, out var rotation, out var position);

                    return Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, scale.Z)) *
                        Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)) *
                        Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, position.Z));
                }

                Matrix4x4Holder ToMatrix4x4Holder(Assimp.Matrix4x4 matrix)
                {
                    return new Matrix4x4Holder(ToMatrix4x4(matrix));
                }

                var globalInverseTransform = scene.RootNode.Transform;

                globalInverseTransform.Inverse();

                Assimp.Node FindNode(Assimp.Node node, string name)
                {
                    if (node.Name == name)
                    {
                        return node;
                    }

                    foreach (var child in node.Children)
                    {
                        node = FindNode(child, name);

                        if (node != null)
                        {
                            return node;
                        }
                    }

                    return null;
                }

                var counter = 0;

                var materialMapping = new List<string>();

                lock(meshMaterialLock)
                {
                    for (var j = 0; j < scene.MaterialCount; j++)
                    {
                        var material = scene.Materials[j];

                        var fileName = Path.GetFileNameWithoutExtension(meshFileName.Replace(".meta", ""));

                        if (material.HasName)
                        {
                            fileName = $"{material.Name}.mat";
                        }
                        else
                        {
                            fileName += $" {++counter}.mat";
                        }

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
                            shader = "Hidden/Shaders/Default/Standard.stsh",
                        };

                        var basePath = Path.GetDirectoryName(meshFileName).Replace(inputPath, "").Substring(1);

                        void AddColor(string name, bool has, Assimp.Color4D color)
                        {
                            var c = Color.White;

                            if (has)
                            {
                                c.r = color.R;
                                c.g = color.G;
                                c.b = color.B;
                                c.a = color.A;
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Color,
                                colorValue = c,
                            });
                        }

                        AddColor("ambientColor", material.HasColorAmbient, material.ColorAmbient);
                        AddColor("diffuseColor", material.HasColorDiffuse, material.ColorDiffuse);
                        AddColor("emissiveColor", material.HasColorEmissive, material.ColorEmissive);
                        AddColor("reflectiveColor", material.HasColorReflective, material.ColorReflective);
                        AddColor("specularColor", material.HasColorSpecular, material.ColorSpecular);
                        AddColor("transparentColor", material.HasColorTransparent, material.ColorTransparent);

                        void AddTexture(string name, bool has, Assimp.TextureSlot slot)
                        {
                            var texturePath = "";

                            var mappingU = TextureWrap.Clamp;
                            var mappingV = TextureWrap.Clamp;

                            if (has)
                            {
                                var pieces = slot.FilePath.Replace("\\", "/").Split("/").ToList();
                                texturePath = slot.FilePath;

                                mappingU = slot.WrapModeU switch
                                {
                                    Assimp.TextureWrapMode.Wrap => TextureWrap.Repeat,
                                    Assimp.TextureWrapMode.Clamp => TextureWrap.Clamp,
                                    Assimp.TextureWrapMode.Mirror => TextureWrap.Mirror,
                                    _ => TextureWrap.Clamp,
                                };

                                mappingV = slot.WrapModeV switch
                                {
                                    Assimp.TextureWrapMode.Wrap => TextureWrap.Repeat,
                                    Assimp.TextureWrapMode.Clamp => TextureWrap.Clamp,
                                    Assimp.TextureWrapMode.Mirror => TextureWrap.Mirror,
                                    _ => TextureWrap.Clamp,
                                };

                                materialMetadata.parameters.Add($"{name}_UMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    textureWrapValue = mappingU,
                                });

                                materialMetadata.parameters.Add($"{name}_VMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    textureWrapValue = mappingV,
                                });

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
                                    Console.WriteLine($"\t\tUnable to find local texture path for {slot.FilePath}");

                                    texturePath = "";
                                }

                                //Console.WriteLine($"\t\tSet Texture {name} to {texturePath}");
                            }
                            else
                            {
                                materialMetadata.parameters.Add($"{name}_UMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    textureWrapValue = mappingU,
                                });

                                materialMetadata.parameters.Add($"{name}_VMapping", new MaterialParameter()
                                {
                                    type = MaterialParameterType.TextureWrap,
                                    textureWrapValue = mappingV,
                                });
                            }

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Texture,
                                textureValue = texturePath,
                            });
                        }

                        AddTexture("ambientTexture", material.HasTextureAmbient, material.TextureAmbient);
                        AddTexture("ambientOcclusionTexture", material.HasTextureAmbientOcclusion, material.TextureAmbientOcclusion);
                        AddTexture("diffuseTexture", material.HasTextureDiffuse, material.TextureDiffuse);
                        AddTexture("displacementTexture", material.HasTextureDisplacement, material.TextureDisplacement);
                        AddTexture("emissiveTexture", material.HasTextureEmissive, material.TextureEmissive);
                        AddTexture("heightTexture", material.HasTextureHeight, material.TextureHeight);
                        AddTexture("lightmapTexture", material.HasTextureLightMap, material.TextureLightMap);
                        AddTexture("normalTexture", material.HasTextureNormal, material.TextureNormal);
                        AddTexture("opacityTexture", material.HasTextureOpacity, material.TextureOpacity);
                        AddTexture("reflectionTexture", material.HasTextureReflection, material.TextureReflection);
                        AddTexture("specularTexture", material.HasTextureSpecular, material.TextureSpecular);

                        if (material.IsPBRMaterial)
                        {
                            AddTexture("baseColorTexture", material.PBR.HasTextureBaseColor, material.PBR.TextureBaseColor);
                            AddTexture("roughnessTexture", material.PBR.HasTextureRoughness, material.PBR.TextureRoughness);
                            AddTexture("metalnessTexture", material.PBR.HasTextureMetalness, material.PBR.TextureMetalness);
                            AddTexture("normalCameraTexture", material.PBR.HasTextureNormalCamera, material.PBR.TextureNormalCamera);
                            AddTexture("emissionColorTexture", material.PBR.HasTextureEmissionColor, material.PBR.TextureEmissionColor);
                        }

                        try
                        {
                            var json = JsonConvert.SerializeObject(materialMetadata, Formatting.Indented, new JsonSerializerSettings()
                            {
                                Converters =
                                {
                                    new StringEnumConverter(),
                                }
                            });

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

                            var json = JsonConvert.SerializeObject(assetHolder, Formatting.Indented);

                            File.WriteAllText($"{target}.meta", json);
                        }
                        catch (Exception)
                        {
                        }

                        Console.WriteLine($"\t\tGenerated material {target}");
                    }
                }

                var transformMatrix = metadata.rotation switch
                {
                    MeshAssetRotation.None => Matrix4x4.Identity,
                    MeshAssetRotation.NinetyPositive => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad(90)),
                    MeshAssetRotation.NinetyNegative => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad(-90)),
                    _ => Matrix4x4.Identity
                };

                transformMatrix = Matrix4x4.CreateScale(metadata.scale) * transformMatrix;

                if (scene.Meshes.Any(x => x.HasBones))
                {
                    transformMatrix = Matrix4x4.Identity;
                }

                Vector3Holder ApplyTransform(Vector3Holder value)
                {
                    return new Vector3Holder(Vector3.Transform(value.ToVector3(), transformMatrix));
                }

                void RegisterNode(Assimp.Node node, MeshAssetNode parent)
                {
                    var newNode = new MeshAssetNode()
                    {
                        name = node.Name,
                        matrix = new Matrix4x4Holder(ToMatrix4x4(node.Transform)),
                        meshIndices = node.MeshIndices,
                    };

                    meshData.rootNode ??= newNode;

                    parent?.children.Add(newNode);

                    foreach (var n in node.Children)
                    {
                        RegisterNode(n, newNode);
                    }
                }

                RegisterNode(scene.RootNode, null);

                foreach (var animation in scene.Animations)
                {
                    var a = new MeshAssetAnimation()
                    {
                        duration = (float)animation.DurationInTicks,
                        ticksPerSecond = (float)animation.TicksPerSecond,
                        name = animation.Name,
                    };

                    foreach (var channel in animation.NodeAnimationChannels)
                    {
                        var c = new MeshAssetAnimationChannel()
                        {
                            nodeName = channel.NodeName,
                            preState = channel.PreState switch
                            {
                                Assimp.AnimationBehaviour.Default => MeshAssetAnimationStateBehaviour.Default,
                                Assimp.AnimationBehaviour.Constant => MeshAssetAnimationStateBehaviour.Constant,
                                Assimp.AnimationBehaviour.Linear => MeshAssetAnimationStateBehaviour.Linear,
                                Assimp.AnimationBehaviour.Repeat => MeshAssetAnimationStateBehaviour.Repeat,
                                _ => MeshAssetAnimationStateBehaviour.Default,
                            },
                            postState = channel.PostState switch
                            {
                                Assimp.AnimationBehaviour.Default => MeshAssetAnimationStateBehaviour.Default,
                                Assimp.AnimationBehaviour.Constant => MeshAssetAnimationStateBehaviour.Constant,
                                Assimp.AnimationBehaviour.Linear => MeshAssetAnimationStateBehaviour.Linear,
                                Assimp.AnimationBehaviour.Repeat => MeshAssetAnimationStateBehaviour.Repeat,
                                _ => MeshAssetAnimationStateBehaviour.Default,
                            },
                            positionKeys = channel.PositionKeys.Select(x => new MeshAssetVectorAnimationKey()
                            {
                                time = (float)x.Time,
                                value = new Vector3Holder(new Vector3(x.Value.X, x.Value.Y, x.Value.Z)),
                            }).ToList(),
                            rotationKeys = channel.RotationKeys.Select(x => new MeshAssetQuaternionAnimationKey()
                            {
                                time = (float)x.Time,
                                value = new Vector4Holder(new Vector4(x.Value.X, x.Value.Y, x.Value.Z, x.Value.W)),
                            }).ToList(),
                            scaleKeys = channel.ScalingKeys.Select(x => new MeshAssetVectorAnimationKey()
                            {
                                time = (float)x.Time,
                                value = new Vector3Holder(new Vector3(x.Value.X, x.Value.Y, x.Value.Z)),
                            }).ToList(),
                        };

                        a.channels.Add(c);
                    }

                    meshData.animations.Add(a);
                }

                foreach (var mesh in scene.Meshes)
                {
                    var m = new MeshAssetMeshInfo
                    {
                        name = mesh.Name,
                        materialGuid = mesh.MaterialIndex >= 0 && mesh.MaterialIndex < materialMapping.Count ? materialMapping[mesh.MaterialIndex] : "",
                        type = mesh.HasBones ? MeshAssetType.Skinned : MeshAssetType.Normal,
                    };

                    var center = (mesh.BoundingBox.Max + mesh.BoundingBox.Min) / 2;

                    var size = (mesh.BoundingBox.Max - mesh.BoundingBox.Min);

                    m.boundsCenter = ApplyTransform(new Vector3Holder(new Vector3(center.X, center.Y, center.Z)));
                    m.boundsExtents = ApplyTransform(new Vector3Holder(new Vector3(size.X, size.Y, size.Z)));

                    switch (mesh.PrimitiveType)
                    {
                        case Assimp.PrimitiveType.Triangle:

                            m.topology = MeshTopology.Triangles;

                            break;

                        case Assimp.PrimitiveType.Line:

                            m.topology = MeshTopology.Lines;

                            break;

                        case Assimp.PrimitiveType.Point:

                            m.topology = MeshTopology.Points;

                            break;

                        default:

                            continue;
                    }

                    m.vertices = mesh.Vertices.Select(x => ApplyTransform(new Vector3Holder(new Vector3(x.X, x.Y, x.Z)))).ToList();
                    m.normals = mesh.Normals.Select(x => ApplyTransform(new Vector3Holder(new Vector3(x.X, x.Y, x.Z)))).ToList();
                    m.tangents = mesh.Tangents.Select(x => ApplyTransform(new Vector3Holder(new Vector3(x.X, x.Y, x.Z)))).ToList();
                    m.bitangents = mesh.BiTangents.Select(x => ApplyTransform(new Vector3Holder(new Vector3(x.X, x.Y, x.Z)))).ToList();
                    m.indices = mesh.Faces.SelectMany(x => x.Indices).ToList();

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

                    var uvCount = mesh.TextureCoordinateChannelCount > uvs.Length ? uvs.Length : mesh.TextureCoordinateChannelCount;

                    for (var j = 0; j < uvCount; j++)
                    {
                        uvs[j].AddRange(mesh.TextureCoordinateChannels[j].Select(x => new Vector2Holder()
                            {
                                x = x.X,
                                y = x.Y,
                            })
                            .ToList());
                    }

                    if (mesh.HasBones)
                    {
                        var boneIndices = new List<Vector4Filler>();
                        var boneWeights = new List<Vector4Filler>();

                        for (var j = 0; j < m.vertices.Count; j++)
                        {
                            boneIndices.Add(new());
                            boneWeights.Add(new());
                        }

                        for (var j = 0; j < mesh.Bones.Count; j++)
                        {
                            var bone = mesh.Bones[j];

                            for (var k = 0; k < bone.VertexWeightCount; k++)
                            {
                                var item = bone.VertexWeights[k];

                                var boneIndex = boneIndices[item.VertexID];
                                var boneWeight = boneWeights[item.VertexID];

                                boneIndex.Add(j);
                                boneWeight.Add(item.Weight);
                            }
                        }

                        m.boneIndices = boneIndices.Select(x => x.ToHolder()).ToList();
                        m.boneWeights = boneWeights.Select(x => x.ToHolder()).ToList();

                        foreach (var bone in mesh.Bones)
                        {
                            m.bones.Add(new()
                            {
                                name = bone.Name,
                                offsetMatrix = ToMatrix4x4Holder(bone.OffsetMatrix),
                            });
                        }
                    }

                    meshData.meshes.Add(m);
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                try
                {
                    var header = new SerializableMeshAssetHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(meshData));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save mesh asset: {e}");
                }
            });
        }
    }
}
