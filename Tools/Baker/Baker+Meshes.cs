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
using System.Threading;

namespace Baker
{
    static partial class Program
    {
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
                Console.WriteLine($"\t{meshFiles[i]}");

                try
                {
                    if (File.Exists(meshFiles[i]) == false)
                    {
                        Console.WriteLine($"\t\tError: {meshFiles[i]} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {meshFiles[i]} doesn't exist");

                    continue;
                }

                var guid = FindGuid<Mesh>(meshFiles[i]);

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(meshFiles[i]));
                var file = Path.GetFileName(meshFiles[i]).Replace(".meta", "");
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                if (ShouldProcessFile(meshFiles[i], outputFile) == false &&
                    ShouldProcessFile(meshFiles[i].Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
                {
                    continue;
                }

                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MeshAssetMetadata metadata;

                try
                {
                    text = File.ReadAllText(meshFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MeshAssetMetadata>(text);

                    metadata.guid = guid;
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    continue;
                }

                using var context = new Assimp.AssimpContext();

                var flags = Assimp.PostProcessSteps.TransformUVCoords |
                    Assimp.PostProcessSteps.GenerateNormals |
                    Assimp.PostProcessSteps.GenerateUVCoords |
                    Assimp.PostProcessSteps.FindDegenerates |
                    Assimp.PostProcessSteps.FindInvalidData |
                    Assimp.PostProcessSteps.FindInstances |
                    Assimp.PostProcessSteps.FixInFacingNormals |
                    Assimp.PostProcessSteps.Triangulate |
                    Assimp.PostProcessSteps.SortByPrimitiveType |
                    Assimp.PostProcessSteps.JoinIdenticalVertices |
                    Assimp.PostProcessSteps.RemoveRedundantMaterials |
                    Assimp.PostProcessSteps.OptimizeMeshes |
                    Assimp.PostProcessSteps.OptimizeGraph |
                    Assimp.PostProcessSteps.CalculateTangentSpace |
                    Assimp.PostProcessSteps.GenerateBoundingBoxes;

                if(metadata.convertUnits)
                {
                    flags |= Assimp.PostProcessSteps.GlobalScale;
                }

                if(metadata.makeLeftHanded)
                {
                    flags |= Assimp.PostProcessSteps.MakeLeftHanded |
                        Assimp.PostProcessSteps.FlipUVs |
                        Assimp.PostProcessSteps.FlipWindingOrder;
                }

                if(metadata.flipUVs)
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

                if(metadata.preTransformVertices)
                {
                    flags |= Assimp.PostProcessSteps.PreTransformVertices;
                }

                if(metadata.debone)
                {
                    flags |= Assimp.PostProcessSteps.Debone;
                }

                if(metadata.limitBoneWeights)
                {
                    flags |= Assimp.PostProcessSteps.LimitBoneWeights;
                }

                if(metadata.splitByBoneCount)
                {
                    flags |= Assimp.PostProcessSteps.SplitByBoneCount;
                }

                Assimp.Scene scene = null;

                try
                {
                    scene = context.ImportFile(meshFiles[i].Replace(".meta", ""), flags);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to import file: {e}");

                    continue;
                }

                var meshData = new SerializableMeshAsset
                {
                    metadata = metadata,
                    materialCount = scene.MaterialCount
                };

                var counter = 0;

                foreach(var material in scene.Materials)
                {
                    string fileName = Path.GetFileNameWithoutExtension(meshFiles[i].Replace(".meta", ""));

                    //TODO: handle refs in-project
                    if(material.HasName && false)
                    {
                        fileName = $"{material.Name}.mat";
                    }
                    else
                    {
                        fileName += $" {++counter}.mat";
                    }

                    var target = Path.Combine(Path.GetDirectoryName(meshFiles[i]), fileName);

                    try
                    {
                        if (File.Exists(target))
                        {
                            continue;
                        }
                    }
                    catch(Exception)
                    {
                    }

                    //Guid collision fix
                    Thread.Sleep(25);

                    var materialMetadata = new MaterialMetadata()
                    {
                        shader = "Shaders/Default/Standard.stsh",
                    };

                    var basePath = Path.GetDirectoryName(meshFiles[i]).Replace(inputPath, "").Substring(1);

                    void AddColor(string name, bool has, Assimp.Color4D color)
                    {
                        var c = Color.White;

                        if(has)
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
                        if (has)
                        {
                            var pieces = slot.FilePath.Replace("\\", "/").Split("/").ToList();
                            var texturePath = slot.FilePath;

                            while(pieces.Count > 0)
                            {
                                try
                                {
                                    var p = Path.Combine(Path.GetDirectoryName(meshFiles[i]), string.Join("/", pieces)).Replace("\\", "/");

                                    if (File.Exists(p))
                                    {
                                        texturePath = string.Join("/", pieces);

                                        if(processedTextures.TryGetValue($"{p}.meta", out var guid))
                                        {
                                            texturePath = guid;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"\t\tUnable to find local texture guid for {p}");

                                            texturePath = "";
                                        }

                                        break;
                                    }
                                }
                                catch(Exception)
                                {
                                }

                                pieces.RemoveAt(0);
                            }

                            if(pieces.Count == 0)
                            {
                                Console.WriteLine($"\t\tUnable to find local texture path for {slot.FilePath}");

                                texturePath = "";
                            }

                            //Console.WriteLine($"\t\tSet Texture {name} to {texturePath}");

                            materialMetadata.parameters.Add(name, new MaterialParameter()
                            {
                                type = MaterialParameterType.Texture,
                                textureValue = texturePath,
                            });
                        }
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

                    if(material.IsPBRMaterial)
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
                    catch(Exception)
                    {
                    }

                    try
                    {
                        var g = FindGuid<Material>($"{target}.meta");

                        var assetHolder = new AssetHolder()
                        {
                            guid = g,
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

                var transformMatrix = metadata.rotation switch
                {
                    MeshAssetRotation.None => Matrix4x4.Identity,
                    MeshAssetRotation.NinetyPositive => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad(90)),
                    MeshAssetRotation.NinetyNegative => Matrix4x4.CreateRotationX(Staple.Math.Deg2Rad(-90)),
                    _ => Matrix4x4.Identity
                };

                Vector3Holder ApplyTransform(Vector3Holder value)
                {
                    return new Vector3Holder(Vector3.Transform(new Vector3(value.x * metadata.scale, value.y * metadata.scale, value.z * metadata.scale), transformMatrix));
                }

                foreach (var mesh in scene.Meshes)
                {
                    var m = new MeshAssetMeshInfo
                    {
                        name = mesh.Name,
                        materialIndex = mesh.MaterialIndex
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
            }
        }
    }
}
