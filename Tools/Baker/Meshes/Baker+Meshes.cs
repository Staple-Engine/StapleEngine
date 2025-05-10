using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                Console.WriteLine($"\t\t -> {outputFile}");

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
                else
                {
                    meshData = ProcessAssimpMesh(metadata, meshFileName.Replace(".meta", ""), inputPath, standardShader, ShaderHasParameter);
                }

                if(meshData == null)
                {
                    return;
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
