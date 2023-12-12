using Assimp;
using MessagePack;
using Newtonsoft.Json;
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
            using var context = new AssimpContext();

            var meshFiles = new List<string>();

            foreach (var extension in meshExtensions)
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

                var guid = FindGuid<MeshAsset>(meshFiles[i]);

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

                var flags = PostProcessSteps.TransformUVCoords |
                    PostProcessSteps.GenerateNormals |
                    PostProcessSteps.GenerateUVCoords |
                    PostProcessSteps.FindDegenerates |
                    PostProcessSteps.FindInvalidData |
                    PostProcessSteps.FindInstances |
                    PostProcessSteps.FixInFacingNormals |
                    PostProcessSteps.Triangulate |
                    PostProcessSteps.SortByPrimitiveType |
                    PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.RemoveRedundantMaterials |
                    PostProcessSteps.OptimizeMeshes |
                    PostProcessSteps.OptimizeGraph;

                if(metadata.makeLeftHanded)
                {
                    flags |= PostProcessSteps.MakeLeftHanded;
                }

                if(metadata.flipUVs)
                {
                    flags |= PostProcessSteps.FlipUVs;
                }

                if (metadata.flipWindingOrder)
                {
                    flags |= PostProcessSteps.FlipWindingOrder;
                }

                if (metadata.splitLargeMeshes)
                {
                    flags |= PostProcessSteps.SplitLargeMeshes;
                }

                if(metadata.preTransformVertices)
                {
                    flags |= PostProcessSteps.PreTransformVertices;
                }

                if(metadata.debone)
                {
                    flags |= PostProcessSteps.Debone;
                }

                if(metadata.limitBoneWeights)
                {
                    flags |= PostProcessSteps.LimitBoneWeights;
                }

                if(metadata.splitByBoneCount)
                {
                    flags |= PostProcessSteps.SplitByBoneCount;
                }

                Assimp.Scene scene = null;

                try
                {
                    scene = context.ImportFile(meshFiles[i].Replace(".meta", ""), flags);
                }
                catch(Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to import file");

                    continue;
                }

                var meshData = new SerializableMeshAsset
                {
                    materialCount = scene.MaterialCount
                };

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
                        case PrimitiveType.Triangle:

                            m.topology = MeshTopology.Triangles;

                            break;

                        case PrimitiveType.Line:

                            m.topology = MeshTopology.Lines;

                            break;

                        case PrimitiveType.Point:

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
