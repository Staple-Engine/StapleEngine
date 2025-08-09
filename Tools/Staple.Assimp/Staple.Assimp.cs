using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Tooling;

public class AssimpImporter : IMeshImporter
{
    private const uint GenBoundingBoxes = 0x80000000;
    private const uint PopulateArmatureData = 0x4000;

    public bool HandlesExtension(string extension)
    {
        return extension switch
        {
            ".3ds" => true,
            ".ase" => true,
            ".bvh" => true,
            ".dae" => true,
            ".ms3d" => true,
            ".ply" => true,
            ".stl" => true,
            _ => false,
        };
    }

    public SerializableMeshAsset ImportMesh(MeshImporterContext context)
    {
        var (metadata, meshFileName, inputPath, standardShader, ShaderHasParameter, meshMaterialLock, processedTextures) =
            (context.metadata, context.meshFileName, context.inputPath, context.standardShader,
            context.ShaderHasParameter, context.materialLock, context.processedTextures);

        unsafe
        {
            using var assimp = Silk.NET.Assimp.Assimp.GetApi();

            var flags = Silk.NET.Assimp.PostProcessSteps.CalculateTangentSpace |
                Silk.NET.Assimp.PostProcessSteps.Triangulate |
                Silk.NET.Assimp.PostProcessSteps.SortByPrimitiveType |
                Silk.NET.Assimp.PostProcessSteps.GenerateSmoothNormals |
                Silk.NET.Assimp.PostProcessSteps.LimitBoneWeights |
                Silk.NET.Assimp.PostProcessSteps.ImproveCacheLocality |
                Silk.NET.Assimp.PostProcessSteps.FindDegenerates |
                Silk.NET.Assimp.PostProcessSteps.FindInvalidData;

            if (metadata.flipUVs)
            {
                flags |= Silk.NET.Assimp.PostProcessSteps.FlipUVs;
            }

            if (metadata.flipWindingOrder)
            {
                flags |= Silk.NET.Assimp.PostProcessSteps.FlipWindingOrder;
            }

            if (metadata.combineSimilarMeshes)
            {
                flags |= Silk.NET.Assimp.PostProcessSteps.OptimizeGraph |
                    Silk.NET.Assimp.PostProcessSteps.OptimizeMeshes;
            }

            Silk.NET.Assimp.Scene* scene = null;

            try
            {
                var store = assimp.CreatePropertyStore();

                assimp.SetImportPropertyInteger(store, "IMPORT_FBX_PRESERVE_PIVOTS", 0);
                assimp.SetImportPropertyInteger(store, "PP_LBW_MAX_WEIGHTS", 4);

                scene = assimp.ImportFileExWithProperties(meshFileName, 0, null, store);

                if (scene == null)
                {
                    Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}");

                    return null;
                }

                /*
                var upAxis = 1;
                var upAxisSign = 1;
                var frontAxis = 2;
                var frontAxisSign = 1;
                var coordAxis = 0;
                var coordAxisSign = 1;
                var unitScale = 1.0f;

                if(scene->MMetaData != null)
                {
                    if(scene->MMetaData->TryGetValue("UpAxis", out int u))
                    {
                        upAxis = u;
                    }

                    if (scene->MMetaData->TryGetValue("UpAxisSign", out int us))
                    {
                        upAxisSign = us;
                    }

                    if (scene->MMetaData->TryGetValue("FrontAxis", out int f))
                    {
                        frontAxis = f;
                    }

                    if (scene->MMetaData->TryGetValue("FrontAxisSign", out int fs))
                    {
                        frontAxisSign = fs;
                    }

                    if (scene->MMetaData->TryGetValue("CoordAxis", out int c))
                    {
                        coordAxis = c;
                    }

                    if (scene->MMetaData->TryGetValue("CoordAxisSign", out int cs))
                    {
                        coordAxisSign = cs;
                    }

                    if (scene->MMetaData->TryGetValue("UnitScaleFactor", out double usf))
                    {
                        unitScale = (float)usf;
                    }
                }

                var upVector = Vector3.Zero;
                var forwardVector = Vector3.Zero;
                var rightVector = Vector3.Zero;

                upVector[upAxis] = upAxisSign * unitScale;
                forwardVector[frontAxis] = frontAxisSign * unitScale;
                rightVector[coordAxis] = coordAxisSign * unitScale;

                var rootMatrix = new Matrix4x4(rightVector.X, rightVector.Y, rightVector.Z, 0,
                    upVector.X, upVector.Y, upVector.Z, 0,
                    forwardVector.X, forwardVector.Y, forwardVector.Z, 0,
                    0, 0, 0, 1);

                scene->MRootNode->MTransformation = rootMatrix * scene->MRootNode->MTransformation;
                */

                /*
                var scaleMatrix = Matrix4x4.CreateScale(metadata.scale);

                scene->MRootNode->MTransformation = Matrix4x4.Transpose(scaleMatrix * Matrix4x4.Transpose(scene->MRootNode->MTransformation));
                */

                assimp.ApplyPostProcessing(scene, (uint)flags | GenBoundingBoxes | PopulateArmatureData);

                assimp.ReleasePropertyStore(store);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}: {e}");

                return null;
            }

            if (scene == null)
            {
                Console.WriteLine($"\t\tError: Failed to import file at {meshFileName}");

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

            Silk.NET.Assimp.Node* FindNode(Silk.NET.Assimp.Node* node, string name)
            {
                if (node->MName == name)
                {
                    return node;
                }

                var children = node->Children();

                foreach (var child in children)
                {
                    var target = FindNode(child, name);

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

                var materials = scene->GetMaterials();

                foreach (var material in materials)
                {
                    var baseName = material->TryGetName(assimp, out var name) ? name : (++counter).ToString();

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

                    if (material->IsTwoSided(assimp))
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

                    var pieces = new Dictionary<string, string>()
                    {
                        { "ambientColor", Silk.NET.Assimp.Assimp.MaterialColorAmbientBase },
                        { "diffuseColor", Silk.NET.Assimp.Assimp.MaterialColorDiffuseBase },
                        { "emissiveColor", Silk.NET.Assimp.Assimp.MaterialColorEmissiveBase },
                        { "reflectiveColor", Silk.NET.Assimp.Assimp.MaterialColorReflectiveBase },
                        { "specularColor", Silk.NET.Assimp.Assimp.MaterialColorSpecularBase },
                        { "transparentColor", Silk.NET.Assimp.Assimp.MaterialColorTransparentBase },
                    };

                    foreach (var pair in pieces)
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
                                            guid = Utilities.FindGuid<Texture>(t);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    texturePath = guid;
                                }
                            }
                            else
                            {
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
                                            var baseP = Path.Combine(Path.GetDirectoryName(meshFileName),
                                                string.Join("/", pieces.Take(pieces.Count - 1)));

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
                                                        Console.WriteLine($"\t\t{Path.GetFileNameWithoutExtension(meshFileName)}: " +
                                                            $"{name} - Unable to find local texture guid for {p}");

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
                                        Console.WriteLine($"\t\t{Path.GetFileNameWithoutExtension(meshFileName)}: " +
                                            $"{name} - Unable to find local texture path for {path}");

                                        texturePath = "";
                                    }
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

                    foreach (var pair in textures)
                    {
                        if (material->TryGetTexture(pair.Value, assimp, out var slot))
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

            void RegisterNode(Silk.NET.Assimp.Node* node, MeshAssetNode parent)
            {
                var nodeName = node->MName.AsString;

                if (nodeCounters.TryGetValue(nodeName, out var counter) == false)
                {
                    counter = 0;

                    nodeCounters.Add(nodeName, counter);
                }

                nodeName = counter == 0 ? nodeName : $"{nodeName}{counter}";

                nodeCounters[nodeName] = counter + 1;

                Matrix4x4.Decompose(Matrix4x4.Transpose(node->MTransformation), out var scale, out var rotation, out var translation);

                var meshIndices = new List<int>(node->MeshIndices());

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

                parent?.children.Add(nodes.Count);

                nodes.Add(newNode);

                var children = node->Children();

                foreach (var child in children)
                {
                    RegisterNode(child, newNode);
                }
            }

            MeshAssetNode rootNode = null;

            if (metadata.scale != 1 || metadata.rotation != MeshAssetRotation.None)
            {
                var rotation = metadata.rotation switch
                {
                    MeshAssetRotation.NinetyPositive => Quaternion.CreateFromAxisAngle(new(1, 0, 0), 90 * Staple.Math.Deg2Rad),
                    MeshAssetRotation.NinetyNegative => Quaternion.CreateFromAxisAngle(new(1, 0, 0), -90 * Staple.Math.Deg2Rad),
                    _ => Quaternion.Identity,
                };

                var scale = Vector3.One * metadata.scale;

                rootNode = new()
                {
                    name = "StapleRoot",
                    position = new(),
                    rotation = new(rotation),
                    scale = new(scale),
                };

                nodes.Add(rootNode);
            }

            RegisterNode(scene->MRootNode, null);

            meshData.nodes = nodes.ToArray();

            #region Meshes
            foreach (var mesh in scene->GetMeshes())
            {
                var m = new MeshAssetMeshInfo
                {
                    name = mesh->MName.AsString,
                    materialGuid = mesh->MMaterialIndex >= 0 && mesh->MMaterialIndex < materialMapping.Count ?
                        materialMapping[(int)mesh->MMaterialIndex] : 
                        AssetDatabase.GetAssetGuid(AssetSerialization.StandardMaterialPath),
                    type = mesh->MNumBones > 0 ? MeshAssetType.Skinned : MeshAssetType.Normal,
                };

                {
                    var center = mesh->MAABB.Center;
                    var size = mesh->MAABB.Size * 2;

                    m.boundsCenter = new Vector3Holder(new Vector3(center.X, center.Y, center.Z));
                    m.boundsExtents = new Vector3Holder(new Vector3(size.X, size.Y, size.Z));
                }

                switch (mesh->MPrimitiveTypes)
                {
                    case (uint)Silk.NET.Assimp.PrimitiveType.Triangle:

                        m.topology = MeshTopology.Triangles;

                        break;

                    default:

                        Console.WriteLine($"\t\tWARNING: Mesh {m.name} of {Path.GetFileNameWithoutExtension(meshFileName)} isn't composed of only triangles ({mesh->MPrimitiveTypes}), adding as empty mesh...");

                        meshData.meshes.Add(m);

                        continue;
                }

                var vertexCount = mesh->MNumVertices;

                var vertices = new List<Vector3Holder>();
                var tangents = new List<Vector3Holder>();
                var bitangents = new List<Vector3Holder>();
                var indices = new List<int>();
                var normals = new Vector3[vertexCount];

                for (var j = 0; j < vertexCount; j++)
                {
                    vertices.Add(ApplyTransform(new Vector3Holder(mesh->MVertices[j])));

                    if (mesh->MTangents != null)
                    {
                        tangents.Add(ApplyNormalTransform(new Vector3Holder(mesh->MTangents[j])));
                    }

                    if (mesh->MBitangents != null)
                    {
                        bitangents.Add(ApplyNormalTransform(new Vector3Holder(mesh->MBitangents[j])));
                    }

                    if (mesh->MNormals != null)
                    {
                        normals[j] = mesh->MNormals[j];
                    }
                }

                var faces = mesh->GetFaces();

                foreach (var face in faces)
                {
                    indices.AddRange(face.GetIndices());
                }

                for (var k = 0; k < 4; k++)
                {
                    if (mesh->TryGetColors(k, out var c))
                    {
                        switch (k)
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
                    if (mesh->TryGetTexCoords(j, out var uv))
                    {
                        uvs[j].AddRange(uv);
                    }
                }

                if (mesh->MNumBones > 0)
                {
                    var boneIndices = new List<Vector4Filler>();
                    var boneWeights = new List<Vector4Filler>();

                    for (var j = 0; j < mesh->MNumVertices; j++)
                    {
                        boneIndices.Add(new());
                        boneWeights.Add(new());
                    }

                    var bones = mesh->GetBones();

                    var invalidBones = new HashSet<string>();

                    for (var j = 0; j < bones.Length; j++)
                    {
                        var bone = bones[j];

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
                            offsetMatrix = Matrix4x4Holder.FromMatrix(Matrix4x4.Transpose(bone->MOffsetMatrix)),
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

            assimp.FreeScene(scene);

            return meshData;
        }
    }
}
