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
            using var context = new Assimp.AssimpContext();

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
                //Guid collision fix
                Thread.Sleep(25);

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
                    Assimp.PostProcessSteps.OptimizeGraph;

                if(metadata.makeLeftHanded)
                {
                    flags |= Assimp.PostProcessSteps.MakeLeftHanded;
                }

                if(metadata.flipUVs)
                {
                    flags |= Assimp.PostProcessSteps.FlipUVs;
                }

                if (metadata.flipWindingOrder)
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
                    var fileName = Path.GetFileNameWithoutExtension(meshFiles[i].Replace(".meta", ""));

                    fileName += $" {++counter}.mat";

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

                    if (material.HasTextureDiffuse)
                    {
                        materialMetadata.parameters.Add("mainTexture", new MaterialParameter()
                        {
                            type = MaterialParameterType.Texture,
                            textureValue = Path.Combine(basePath, material.TextureDiffuse.FilePath).Replace("\\", "/"),
                        });
                    }

                    var mainColor = Color.White;

                    if(material.HasColorDiffuse)
                    {
                        mainColor.r = material.ColorDiffuse.R;
                        mainColor.g = material.ColorDiffuse.G;
                        mainColor.b = material.ColorDiffuse.B;
                        mainColor.a = material.ColorDiffuse.A;
                    }

                    materialMetadata.parameters.Add("mainColor", new MaterialParameter()
                    {
                        type = MaterialParameterType.Color,
                        colorValue = mainColor,
                    });

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

                foreach (var mesh in scene.Meshes)
                {
                    var m = new MeshAssetMeshInfo
                    {
                        name = mesh.Name,
                        materialIndex = mesh.MaterialIndex
                    };

                    var center = (mesh.BoundingBox.Max + mesh.BoundingBox.Min) / 2;

                    var size = (mesh.BoundingBox.Max - mesh.BoundingBox.Min);

                    m.boundsCenter = new Vector3Holder(new Vector3(center.X, center.Y, center.Z));
                    m.boundsExtents = new Vector3Holder(new Vector3(size.X, size.Y, size.Z));

                    switch(mesh.PrimitiveType)
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

                    m.vertices = mesh.Vertices.Select(x => new Vector3Holder(new Vector3(x.X, x.Y, x.Z))).ToList();
                    m.normals = mesh.Normals.Select(x => new Vector3Holder(new Vector3(x.X, x.Y, x.Z))).ToList();
                    m.tangents = mesh.Tangents.Select(x => new Vector3Holder(new Vector3(x.X, x.Y, x.Z))).ToList();
                    m.bitangents = mesh.BiTangents.Select(x => new Vector3Holder(new Vector3(x.X, x.Y, x.Z))).ToList();
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
                        }).ToList());
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
