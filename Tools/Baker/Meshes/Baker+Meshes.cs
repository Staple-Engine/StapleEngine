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

namespace Baker;

#region Classes
class Vector4Filler
{
    public float x;
    public float y;
    public float z;
    public float w;

    public int index = 0;

    public float this[int index]
    {
        get
        {
            return index switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => 0,
            };
        }

        set
        {
            switch(index)
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
            }
        }
    }

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

    public Vector4Holder ToHolderNormalized()
    {
        var total = x + y + z + w;

        return new()
        {
            x = x / total,
            y = y / total,
            z = z / total,
            w = w / total,
        };
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
#endregion

static partial class Program
{
    private static readonly Lock meshMaterialLock = new();

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

            {
                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }
            }

            outputFile = outputFile.Replace("\\", "/").Replace("/./", "/");

            if (ShouldProcessFile(meshFileName, outputFile) == false &&
                ShouldProcessFile(meshFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(meshFileName.Replace(".meta", "")), () =>
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

                SerializableMeshAsset meshData = null;

                if(meshFileName.EndsWith(".gltf.meta") || meshFileName.EndsWith(".glb.meta"))
                {
                    meshData = ProcessSharpGLTFMesh(metadata, meshFileName.Replace(".meta", ""), inputPath, standardShader, ShaderHasParameter);
                }
                else if(meshFileName.EndsWith(".fbx.meta"))
                {
                    meshData = ProcessUFBXMesh(metadata, meshFileName.Replace(".meta", ""), inputPath, standardShader, ShaderHasParameter);
                }
                else
                {
                    meshData = ProcessAssimpMesh(metadata, meshFileName.Replace(".meta", ""), inputPath, standardShader, ShaderHasParameter);
                }

                if(meshData == null)
                {
                    return;
                }

                foreach (var mesh in meshData.meshes)
                {
                    if ((mesh.tangents?.Count ?? 0) == 0 &&
                        mesh.topology == MeshTopology.Triangles &&
                        (mesh.UV1?.Count ?? 0) > 0 &&
                        (mesh.normals?.Count ?? 0) > 0)
                    {
                        var tangents = new Vector3[mesh.vertices.Count];
                        var bitangents = new Vector3[mesh.vertices.Count];

                        for (var j = 0; j < mesh.indices.Count; j += 3)
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

                        for (var j = 0; j < mesh.vertices.Count; j++)
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

                            mesh.tangents.Add(new(tangent));
                            mesh.bitangents.Add(new(bitangent));
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
