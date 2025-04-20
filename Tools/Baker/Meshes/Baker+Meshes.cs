using MessagePack;
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
using System.Threading;

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
    private static readonly Lock meshMaterialLock = new();

    private const uint GenBoundingBoxes = 0x80000000;

    private static unsafe void ProcessMeshes(AppPlatform platform, string inputPath, string outputPath)
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

        RenderWindow.CurrentRenderer = RendererType.Direct3D11;

        var standardShader = ResourceManager.instance.LoadShaderData(AssetSerialization.StandardShaderGUID);

        bool ShaderHasParameter(string name)
        {
            return standardShader?.metadata.uniforms.Any(x => x.name == name) ?? false;
        }

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

            outputFile = outputFile.Replace("\\", "/").Replace("/./", "/");

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

                var assimp = Silk.NET.Assimp.Assimp.GetApi();

                var flags = Silk.NET.Assimp.PostProcessSteps.CalculateTangentSpace |
                    Silk.NET.Assimp.PostProcessSteps.JoinIdenticalVertices |
                    Silk.NET.Assimp.PostProcessSteps.Triangulate |
                    Silk.NET.Assimp.PostProcessSteps.GenerateSmoothNormals |
                    Silk.NET.Assimp.PostProcessSteps.LimitBoneWeights |
                    Silk.NET.Assimp.PostProcessSteps.ImproveCacheLocality |
                    Silk.NET.Assimp.PostProcessSteps.RemoveRedundantMaterials |
                    Silk.NET.Assimp.PostProcessSteps.FixInFacingNormals |
                    Silk.NET.Assimp.PostProcessSteps.SortByPrimitiveType |
                    Silk.NET.Assimp.PostProcessSteps.FindDegenerates |
                    Silk.NET.Assimp.PostProcessSteps.FindInvalidData |
                    Silk.NET.Assimp.PostProcessSteps.GenerateUVCoords |
                    Silk.NET.Assimp.PostProcessSteps.TransformUVCoords |
                    Silk.NET.Assimp.PostProcessSteps.FindInstances |
                    Silk.NET.Assimp.PostProcessSteps.OptimizeMeshes |
                    Silk.NET.Assimp.PostProcessSteps.OptimizeGraph;

                if (metadata.makeLeftHanded)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.MakeLeftHanded;
                }

                if (metadata.flipUVs)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.FlipUVs;
                }

                if (metadata.flipWindingOrder)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.FlipWindingOrder;
                }

                if (metadata.splitLargeMeshes)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.SplitLargeMeshes;
                }

                if (metadata.preTransformVertices)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.PreTransformVertices;
                }

                if (metadata.debone)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.Debone;
                }

                if (metadata.splitByBoneCount)
                {
                    flags |= Silk.NET.Assimp.PostProcessSteps.SplitByBoneCount;
                }

                Silk.NET.Assimp.Scene* scene = null;

                try
                {
                    scene = assimp.ImportFile(meshFileName.Replace(".meta", ""), (uint)flags | GenBoundingBoxes);
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

                Silk.NET.Assimp.Node* FindNode(Silk.NET.Assimp.Node* node, string name, bool ignoreRoot)
                {
                    if (node->MName == name &&
                        (ignoreRoot == false || node != scene->MRootNode))
                    {
                        return node;
                    }

                    var children = node->Children();

                    foreach (var child in children)
                    {
                        var target = FindNode(child, name, false);

                        if (target != null)
                        {
                            return target;
                        }
                    }

                    return null;
                }

                var counter = 0;

                var materialMapping = new List<string>();
                var materialEmbeddedTextures = new Dictionary<string, string>();

                lock(meshMaterialLock)
                {
                    var materials = scene->GetMaterials();

                    foreach(var material in materials)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(meshFileName.Replace(".meta", ""));

                        if (material->TryGetName(assimp, out var name))
                        {
                            fileName = $"{name}.{AssetSerialization.MaterialExtension}";
                        }
                        else
                        {
                            fileName += $" {++counter}.{AssetSerialization.MaterialExtension}";
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
                            shader = AssetSerialization.StandardShaderGUID,
                        };

                        if(material->IsTwoSided(assimp))
                        {
                            materialMetadata.cullingMode = CullingMode.None;
                        }

                        var basePath = Path.GetDirectoryName(meshFileName).Replace(inputPath, "");
                        
                        if(basePath.Length > 0)
                        {
                            basePath = basePath.Substring(1);
                        }

                        foreach(var p in standardShader?.metadata?.instanceParameters ?? [])
                        {
                            switch(p.type)
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
                            if(ShaderHasParameter(name) == false)
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

                        var pieces = new Dictionary<string, string>()
                        {
                            { "ambientColor", Silk.NET.Assimp.Assimp.MaterialColorAmbientBase },
                            { "diffuseColor", Silk.NET.Assimp.Assimp.MaterialColorDiffuseBase },
                            { "emissiveColor", Silk.NET.Assimp.Assimp.MaterialColorEmissiveBase },
                            { "reflectiveColor", Silk.NET.Assimp.Assimp.MaterialColorReflectiveBase },
                            { "specularColor", Silk.NET.Assimp.Assimp.MaterialColorSpecularBase },
                            { "transparentColor", Silk.NET.Assimp.Assimp.MaterialColorTransparentBase },
                        };

                        foreach(var pair in pieces)
                        {
                            if (material->TryGetColor(pair.Value, assimp, out var color))
                            {
                                AddColor(pair.Key, true, color);
                            }
                        }

                        var textures = new Dictionary<string, Silk.NET.Assimp.TextureType>()
                        {
                            { "ambientTexture", Silk.NET.Assimp.TextureType.Ambient },
                            { "ambientOcclusionTexture", Silk.NET.Assimp.TextureType.AmbientOcclusion },
                            { "diffuseTexture", Silk.NET.Assimp.TextureType.Diffuse },
                            { "displacementTexture", Silk.NET.Assimp.TextureType.Displacement },
                            { "emissiveTexture", Silk.NET.Assimp.TextureType.Emissive },
                            { "heightTexture", Silk.NET.Assimp.TextureType.Height },
                            { "lightmapTexture", Silk.NET.Assimp.TextureType.Lightmap },
                            { "normalTexture", Silk.NET.Assimp.TextureType.Normals },
                            { "opacityTexture", Silk.NET.Assimp.TextureType.Opacity },
                            { "reflectionTexture", Silk.NET.Assimp.TextureType.Reflection },
                            { "specularTexture", Silk.NET.Assimp.TextureType.Specular },
                        };

                        void AddTexture(string name, bool has, AssimpExtensions.TextureSlot slot)
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
                                var path = slot.path;

                                if (path.StartsWith('*'))
                                {
                                    if(int.TryParse(path.AsSpan(1), out var textureIndex) == false)
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

                                            for(var j = 0; j < 9; j++)
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
                                        }
                                        catch (Exception)
                                        {
                                        }

                                        texturePath = Path.Combine(basePath, texturePath).Replace("\\", "/");
                                    }
                                }
                                else
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
                                mappingU = slot.mapModeU;
                                mappingV = slot.mapModeV;
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

                        foreach(var pair in textures)
                        {
                            if(material->TryGetTexture(pair.Value, assimp, out var slot))
                            {
                                AddTexture(pair.Key, true, slot);
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

                Vector3Holder ApplyTransform(Vector3Holder value)
                {
                    return value;
                }

                var nodes = new List<MeshAssetNode>();

                var nodeCounters = new Dictionary<string, int>();
                var nodeToName = new Dictionary<nint, string>();
                var nodeToIndex = new Dictionary<nint, int>();
                var nodeParents = new Dictionary<nint, nint>();
                var localNodes = new Dictionary<int, nint>();

                void RegisterNode(Silk.NET.Assimp.Node* node)
                {
                    var children = node->Children();

                    if (node == scene->MRootNode)
                    {
                        foreach(var child in children)
                        {
                            RegisterNode(child);
                        }

                        return;
                    }

                    Matrix4x4.Decompose(Matrix4x4.Transpose(node->MTransformation), out var scale, out var rotation, out var translation);

                    var meshIndices = new List<int>(node->MeshIndices());

                    var nodeName = node->MName.AsString;

                    if (nodeCounters.TryGetValue(nodeName, out var counter))
                    {
                        counter++;

                        nodeCounters.AddOrSetKey(nodeName, counter);
                    }
                    else
                    {
                        counter = 0;

                        nodeCounters.Add(nodeName, counter);
                    }

                    nodeName = counter == 0 ? nodeName : $"{nodeName}{counter}";

                    var newNode = new MeshAssetNode()
                    {
                        name = nodeName,
                        meshIndices = meshIndices,
                        position = new Vector3Holder(translation),
                        scale = new Vector3Holder(scale),
                        rotation = new Vector3Holder(rotation),
                    };

                    var currentIndex = nodes.Count;

                    nodeToName.Add((nint)node, nodeName);
                    nodeToIndex.Add((nint)node, currentIndex);
                    localNodes.Add(currentIndex, (nint)node);

                    if (node->MParent != null && node->MParent != scene->MRootNode)
                    {
                        nodeParents.Add((nint)node, (nint)node->MParent);
                    }

                    nodes.Add(newNode);

                    foreach(var child in children)
                    {
                        RegisterNode(child);
                    }
                }

                RegisterNode(scene->MRootNode);

                foreach(var pair in localNodes)
                {
                    if(nodeParents.TryGetValue(pair.Value, out var parent) &&
                        parent != nint.Zero &&
                        nodeToIndex.TryGetValue(parent, out var parentNodeIndex))
                    {
                        var parentNode = nodes[parentNodeIndex];

                        parentNode.children.Add(pair.Key);
                    }
                }

                meshData.nodes = nodes.ToArray();

                string GetNodeName(string name)
                {
                    var node = FindNode(scene->MRootNode, name, true);

                    if (node != null &&
                        nodeToName.TryGetValue((nint)node, out var newNodeName))
                    {
                        return newNodeName;
                    }

                    return name;
                }

                var animations = scene->Animations();

                foreach(var animation in animations)
                {
                    var a = new MeshAssetAnimation()
                    {
                        duration = (float)animation->MDuration,
                        ticksPerSecond = (float)animation->MTicksPerSecond,
                        name = animation->MName.AsString,
                    };

                    var channels = animation->Channels();

                    foreach (var channel in channels)
                    {
                        var positionKeys = channel->PositionKeys()
                            .Select(x => new MeshAssetVectorAnimationKey()
                            {
                                time = (float)x.MTime,
                                value = new(x.MValue),
                            })
                            .ToList();

                        var rotationKeys = channel->RotationKeys()
                            .Select(x => new MeshAssetQuaternionAnimationKey()
                            {
                                time = (float)x.MTime,
                                value = new(x.MValue),
                            })
                            .ToList();

                        var scaleKeys = channel->ScaleKeys()
                            .Select(x => new MeshAssetVectorAnimationKey()
                            {
                                time = (float)x.MTime,
                                value = new(x.MValue),
                            })
                            .ToList();

                        var c = new MeshAssetAnimationChannel()
                        {
                            nodeName = GetNodeName(channel->MNodeName.AsString),
                            preState = channel->MPreState switch
                            {
                                Silk.NET.Assimp.AnimBehaviour.Default => MeshAssetAnimationStateBehaviour.Default,
                                Silk.NET.Assimp.AnimBehaviour.Constant => MeshAssetAnimationStateBehaviour.Constant,
                                Silk.NET.Assimp.AnimBehaviour.Linear => MeshAssetAnimationStateBehaviour.Linear,
                                Silk.NET.Assimp.AnimBehaviour.Repeat => MeshAssetAnimationStateBehaviour.Repeat,
                                _ => MeshAssetAnimationStateBehaviour.Default,
                            },
                            postState = channel->MPostState switch
                            {
                                Silk.NET.Assimp.AnimBehaviour.Default => MeshAssetAnimationStateBehaviour.Default,
                                Silk.NET.Assimp.AnimBehaviour.Constant => MeshAssetAnimationStateBehaviour.Constant,
                                Silk.NET.Assimp.AnimBehaviour.Linear => MeshAssetAnimationStateBehaviour.Linear,
                                Silk.NET.Assimp.AnimBehaviour.Repeat => MeshAssetAnimationStateBehaviour.Repeat,
                                _ => MeshAssetAnimationStateBehaviour.Default,
                            },
                            positionKeys = positionKeys,
                            rotationKeys = rotationKeys,
                            scaleKeys = scaleKeys,
                        };

                        a.channels.Add(c);
                    }

                    meshData.animations.Add(a);
                }

                var meshes = scene->GetMeshes();

                foreach (var mesh in meshes)
                {
                    var m = new MeshAssetMeshInfo
                    {
                        name = mesh->MName.AsString,
                        materialGuid = mesh->MMaterialIndex >= 0 && mesh->MMaterialIndex < materialMapping.Count ? materialMapping[(int)mesh->MMaterialIndex] : "",
                        type = mesh->MNumBones > 0 ? MeshAssetType.Skinned : MeshAssetType.Normal,
                    };

                    var center = mesh->MAABB.Center;
                    var size = mesh->MAABB.Size;

                    m.boundsCenter = ApplyTransform(new Vector3Holder(new Vector3(center.X, center.Y, center.Z)));
                    m.boundsExtents = ApplyTransform(new Vector3Holder(new Vector3(size.X, size.Y, size.Z)));

                    switch (mesh->MPrimitiveTypes)
                    {
                        case (uint)Silk.NET.Assimp.PrimitiveType.Triangle:

                            m.topology = MeshTopology.Triangles;

                            break;

                        case (uint)Silk.NET.Assimp.PrimitiveType.Line:

                            m.topology = MeshTopology.Lines;

                            break;

                        case (uint)Silk.NET.Assimp.PrimitiveType.Point:

                            m.topology = MeshTopology.Points;

                            break;

                        default:

                            continue;
                    }

                    var vertices = new List<Vector3Holder>();
                    var tangents = new List<Vector3Holder>();
                    var bitangents = new List<Vector3Holder>();
                    var indices = new List<int>();
                    var normals = mesh->MNormals != null ? new Vector3[mesh->MNumVertices] : [];

                    for(var j = 0; j < mesh->MNumVertices; j++)
                    {
                        vertices.Add(ApplyTransform(new Vector3Holder(mesh->MVertices[j])));

                        if(mesh->MTangents != null)
                        {
                            tangents.Add(ApplyTransform(new Vector3Holder(mesh->MTangents[j])));
                        }

                        if(mesh->MBitangents != null)
                        {
                            bitangents.Add(ApplyTransform(new Vector3Holder(mesh->MBitangents[j])));
                        }

                        if(mesh->MNormals != null)
                        {
                            normals[j] = mesh->MNormals[j];
                        }
                    }

                    var faces = mesh->GetFaces();

                    foreach(var face in faces)
                    {
                        indices.AddRange(face.GetIndices());
                    }

                    for(var k = 0; k < 4; k++)
                    {
                        if (mesh->TryGetColors(k, out var c))
                        {
                            switch(k)
                            {
                                case 0:

                                    m.colors.AddRange(c);

                                    break;

                                case 1:

                                    m.colors2.AddRange(c);

                                    break;

                                case 2:

                                    m.colors3.AddRange(c);

                                    break;

                                case 3:

                                    m.colors4.AddRange(c);

                                    break;
                            }
                        }
                    }

                    m.vertices = vertices;

                    m.tangents = tangents;

                    m.bitangents = bitangents;

                    m.indices = indices;

                    if(metadata.regenerateNormals)
                    {
                        var v = m.vertices
                            .Select(x => x.ToVector3())
                            .ToArray();

                        normals = Mesh.GenerateNormals(v, CollectionsMarshal.AsSpan(m.indices), metadata.useSmoothNormals);
                    }

                    m.normals = normals
                        .Select(x => ApplyTransform(new Vector3Holder(x)))
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
                        if(mesh->TryGetTexCoords(j, out var uv))
                        {
                            uvs[j].AddRange(uv);
                        }
                    }

                    if (mesh->MNumBones > 0)
                    {
                        var boneIndices = new List<Vector4Filler>();
                        var boneWeights = new List<Vector4Filler>();

                        for (var j = 0; j < m.vertices.Count; j++)
                        {
                            boneIndices.Add(new());
                            boneWeights.Add(new());
                        }

                        var bones = mesh->GetBones();

                        for (var j = 0; j < bones.Length; j++)
                        {
                            var bone = bones[j];

                            Matrix4x4.Decompose(Matrix4x4.Transpose(bone->MOffsetMatrix), out var scale, out var rotation, out var translation);

                            m.bones.Add(new()
                            {
                                name = GetNodeName(bone->MName.AsString),
                                offsetPosition = new Vector3Holder(translation),
                                offsetScale = new Vector3Holder(scale),
                                offsetRotation = new Vector3Holder(rotation),
                            });

                            var weights = bone->GetWeights();

                            for (var k = 0; k < weights.Length; k++)
                            {
                                var item = weights[k];

                                var boneIndex = boneIndices[(int)item.MVertexId];
                                var boneWeight = boneWeights[(int)item.MVertexId];

                                boneIndex.Add(j);
                                boneWeight.Add(item.MWeight);
                            }
                        }

                        m.boneIndices = boneIndices
                            .Select(x => x.ToHolder())
                            .ToList();

                        m.boneWeights = boneWeights
                            .Select(x => x.ToHolder())
                            .ToList();
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

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(meshData));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save mesh asset: {e}");
                }
            });
        }
    }
}
