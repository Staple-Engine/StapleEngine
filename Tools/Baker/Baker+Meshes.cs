using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using Staple.Tooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Baker;

static partial class Program
{
    private static readonly Lock meshMaterialLock = new();

    private static string ResolveMeshTexturePath(string path, string meshFileName)
    {
        Console.WriteLine($"\t\tResolving texture {path}");

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

        var texturePath = path;

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

        return texturePath;
    }

    private static unsafe void ProcessMeshes(AppPlatform platform, string inputPath, string outputPath)
    {
        var meshFiles = new List<string>();

        #region Prepare Tasks
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

        LogMessage($"Processing {meshFiles.Count} meshes...");

        RenderWindow.CurrentRenderer = RendererType.Direct3D12;

        var standardShader = ResourceManager.instance.LoadShaderData(AssetDatabase.GetAssetGuid(AssetSerialization.StandardShaderPath));

        if(standardShader == null)
        {
            LogMessage($"\t\tError: Failed to load standard shader");

            return;
        }

        bool ShaderHasParameter(string name)
        {
            return standardShader?.metadata.uniforms.Any(x => x.name == name) ?? false;
        }

        List<IMeshImporter> importers =
            [
                new UFXImporter(),
                new SharpGLTFImporter(),
            ];

        for (var i = 0; i < meshFiles.Count; i++)
        {
            var meshFileName = meshFiles[i];

            //Console.WriteLine($"\t{meshFileName}");

            try
            {
                if (File.Exists(meshFileName) == false)
                {
                    LogMessage($"\t\tError: {meshFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                LogMessage($"\t\tError: {meshFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Mesh>(meshFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(meshFileName));
            var file = Path.GetFileName(meshFileName).Replace(".meta", "");
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            {
                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }
            }

            outputFile = outputFile.Replace("\\", "/").Replace("/./", "/");

            if (ReportChangedAsset(meshFileName, outputFile))
            {
                continue;
            }

            if (ShouldProcessFile(meshFileName, outputFile) == false &&
                ShouldProcessFile(meshFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Main.Dispatch(Path.GetFileName(meshFileName.Replace(".meta", "")), () =>
            {
                #endregion
                //Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MeshAssetMetadata metadata;

                try
                {
                    text = File.ReadAllText(meshFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file {meshFileName}");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MeshAssetMetadata>(text);

                    metadata.guid = guid;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted for {meshFileName}: {e}");

                    return;
                }

                var context = new MeshImporterContext()
                {
                    inputPath = inputPath,
                    materialLock = meshMaterialLock,
                    meshFileName = meshFileName.Replace(".meta", ""),
                    metadata = metadata,
                    processedTextures = processedTextures,
                    shaderHasParameter = ShaderHasParameter,
                    standardShader = standardShader,
                    resolveTexturePath = ResolveMeshTexturePath,
                };

                SerializableMeshAsset meshData = null;

                var extension = Path.GetExtension(meshFileName.Replace(".meta", "")).ToLowerInvariant();

                foreach(var importer in importers)
                {
                    if(importer.HandlesExtension(extension))
                    {
                        meshData = importer.ImportMesh(context);

                        break;
                    }
                }

                if(meshData == null)
                {
                    return;
                }

                foreach (var mesh in meshData.meshes)
                {
                    if (string.IsNullOrEmpty(mesh.materialGuid))
                    {
                        mesh.materialGuid = AssetDatabase.GetAssetGuid("Hidden/Materials/Standard.material") ?? mesh.materialGuid;
                    }
                }

                //Do this for OBJ only right now
                if (metadata.combineSimilarMeshes && extension == ".obj")
                {
                    var combinableMeshes = new Dictionary<string, Dictionary<MeshAssetComponent, List<MeshAssetMeshInfo>>>();

                    foreach (var mesh in meshData.meshes)
                    {
                        if (combinableMeshes.TryGetValue(mesh.materialGuid, out var contents) == false)
                        {
                            contents = [];

                            combinableMeshes.Add(mesh.materialGuid, contents);
                        }

                        if (contents.TryGetValue(mesh.Components, out var meshes) == false)
                        {
                            meshes = [];

                            contents.Add(mesh.Components, meshes);
                        }

                        meshes.Add(mesh);
                    }

                    var newMeshes = new List<MeshAssetMeshInfo>();

                    foreach (var pair in combinableMeshes)
                    {
                        foreach (var meshPair in pair.Value)
                        {
                            if (meshPair.Value.Count == 1)
                            {
                                newMeshes.Add(meshPair.Value[0]);

                                continue;
                            }

                            var first = meshPair.Value[0];

                            var newMesh = new MeshAssetMeshInfo()
                            {
                                name = first.name,
                                topology = first.topology,
                                type = first.type,
                                materialGuid = first.materialGuid,
                            };

                            var vertexCount = meshPair.Value.Select(x => x.vertices.Length).Sum();

                            newMesh.vertices = new Vector3Holder[vertexCount];
                            newMesh.indices = new int[meshPair.Value.Select(x => x.indices.Length).Sum()];

                            newMesh.normals = meshPair.Key.HasFlag(MeshAssetComponent.Normal) ? new Vector3Holder[vertexCount] : [];
                            newMesh.tangents = meshPair.Key.HasFlag(MeshAssetComponent.Tangent) ? new Vector3Holder[vertexCount] : [];
                            newMesh.bitangents = meshPair.Key.HasFlag(MeshAssetComponent.Bitangent) ? new Vector3Holder[vertexCount] : [];
                            newMesh.colors = meshPair.Key.HasFlag(MeshAssetComponent.Color1) ? new Vector4Holder[vertexCount] : [];
                            newMesh.colors2 = meshPair.Key.HasFlag(MeshAssetComponent.Color2) ? new Vector4Holder[vertexCount] : [];
                            newMesh.colors3 = meshPair.Key.HasFlag(MeshAssetComponent.Color3) ? new Vector4Holder[vertexCount] : [];
                            newMesh.colors4 = meshPair.Key.HasFlag(MeshAssetComponent.Color4) ? new Vector4Holder[vertexCount] : [];
                            newMesh.UV1 = meshPair.Key.HasFlag(MeshAssetComponent.UV1) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV2 = meshPair.Key.HasFlag(MeshAssetComponent.UV2) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV3 = meshPair.Key.HasFlag(MeshAssetComponent.UV3) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV4 = meshPair.Key.HasFlag(MeshAssetComponent.UV4) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV5 = meshPair.Key.HasFlag(MeshAssetComponent.UV5) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV6 = meshPair.Key.HasFlag(MeshAssetComponent.UV6) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV7 = meshPair.Key.HasFlag(MeshAssetComponent.UV7) ? new Vector2Holder[vertexCount] : [];
                            newMesh.UV8 = meshPair.Key.HasFlag(MeshAssetComponent.UV8) ? new Vector2Holder[vertexCount] : [];

                            var startVertex = 0;
                            var startIndex = 0;

                            static void Copy<T>(Span<T> from, Span<T> to, int start, int length) where T: unmanaged
                            {
                                if(to.Length == 0)
                                {
                                    return;
                                }

                                from.CopyTo(to.Slice(start, length));
                            }

                            foreach (var submesh in meshPair.Value)
                            {
                                Copy(submesh.vertices.AsSpan(), newMesh.vertices.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.normals.AsSpan(), newMesh.normals.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.tangents.AsSpan(), newMesh.tangents.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.bitangents.AsSpan(), newMesh.bitangents.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.colors.AsSpan(), newMesh.colors.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.colors2.AsSpan(), newMesh.colors2.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.colors3.AsSpan(), newMesh.colors3.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.colors4.AsSpan(), newMesh.colors4.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV1.AsSpan(), newMesh.UV1.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV2.AsSpan(), newMesh.UV2.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV3.AsSpan(), newMesh.UV3.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV4.AsSpan(), newMesh.UV4.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV5.AsSpan(), newMesh.UV5.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV6.AsSpan(), newMesh.UV6.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV7.AsSpan(), newMesh.UV7.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.UV8.AsSpan(), newMesh.UV8.AsSpan(), startVertex, submesh.vertices.Length);
                                Copy(submesh.indices.Select(x => x + startVertex).ToArray().AsSpan(), newMesh.indices,
                                    startIndex, submesh.indices.Length);

                                startVertex += submesh.vertices.Length;
                                startIndex += submesh.indices.Length;
                            }

                            var p = newMesh.vertices.Select(x => x.ToVector3()).ToArray();

                            var aabb = AABB.CreateFromPoints(p);

                            newMesh.boundsCenter = new(aabb.center);
                            newMesh.boundsExtents = new(aabb.size);

                            newMeshes.Add(newMesh);
                        }
                    }

                    meshData.meshes = newMeshes.ToArray();

                    meshData.nodes = 
                        [
                            new()
                            {
                                name = "root",
                                meshIndices = newMeshes.Select((x, xIndex) => xIndex).ToList(),
                                position = new(),
                                rotation = new(Quaternion.Identity),
                                scale = new(Vector3.One),
                            }
                        ];
                }

                meshData = MeshOptimization.OptimizeMeshAsset(meshData);

                foreach (var mesh in meshData.meshes)
                {
                    if ((mesh.tangents?.Length ?? 0) == 0 &&
                        mesh.topology == MeshTopology.Triangles &&
                        (mesh.UV1?.Length ?? 0) > 0 &&
                        (mesh.normals?.Length ?? 0) > 0)
                    {
                        var tangents = new Vector3[mesh.vertices.Length];
                        var bitangents = new Vector3[mesh.vertices.Length];

                        for (var j = 0; j < mesh.indices.Length; j += 3)
                        {
                            var indices = (mesh.indices[j], mesh.indices[j + 1], mesh.indices[j + 2]);

                            var vectors = (mesh.vertices[indices.Item1].ToVector3(),
                                mesh.vertices[indices.Item2].ToVector3(),
                                mesh.vertices[indices.Item3].ToVector3());

                            var uvs = (mesh.UV1[indices.Item1].ToVector2(),
                                mesh.UV1[indices.Item2].ToVector2(),
                                mesh.UV1[indices.Item3].ToVector2());

                            var edge1 = vectors.Item2 - vectors.Item1;
                            var edge2 = vectors.Item3 - vectors.Item1;

                            var uvDelta1 = uvs.Item2 - uvs.Item1;
                            var uvDelta2 = uvs.Item3 - uvs.Item1;

                            var f = 1.0f / (uvDelta1.X * uvDelta2.Y - uvDelta2.X * uvDelta1.Y);

                            var tangent = Vector3.Normalize(f * (uvDelta2.Y * edge1 - uvDelta1.Y * edge2));
                            var bitangent = Vector3.Normalize(f * (-uvDelta2.X * edge1 + uvDelta1.X * edge2));

                            tangents[indices.Item1] += tangent;
                            tangents[indices.Item2] += tangent;
                            tangents[indices.Item3] += tangent;

                            bitangents[indices.Item1] += bitangent;
                            bitangents[indices.Item2] += bitangent;
                            bitangents[indices.Item3] += bitangent;
                        }

                        mesh.tangents = new Vector3Holder[mesh.vertices.Length];
                        mesh.bitangents = new Vector3Holder[mesh.vertices.Length];

                        for (var j = 0; j < mesh.vertices.Length; j++)
                        {
                            var normal = mesh.normals[j].ToVector3();
                            var t = Vector3.Normalize(tangents[j]);
                            var b = Vector3.Normalize(bitangents[j]);

                            var tangent = Vector3.Normalize(t - normal * Vector3.Dot(normal, t));

                            var bitangent = Vector3.Cross(normal, Vector3.Normalize(tangent));

                            if(Vector3.Dot(bitangent, b) < 0)
                            {
                                bitangent = -bitangent;
                            }

                            mesh.tangents[j] = new(tangent);
                            mesh.bitangents[j] = new(bitangent);
                        }
                    }
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
