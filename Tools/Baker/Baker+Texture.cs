using MessagePack;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessTextures(string shadercPath, string inputPath, string outputPath, Renderer renderer)
        {
            var textureFiles = new List<string>();

            foreach (var extension in textureExtensions)
            {
                try
                {
                    textureFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine($"Processing {textureFiles.Count} textures...");

            for (var i = 0; i < textureFiles.Count; i++)
            {
                Console.WriteLine($"\t{textureFiles[i].Replace(".meta", "")}");

                try
                {
                    if (File.Exists(textureFiles[i].Replace(".meta", "")) == false)
                    {
                        Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                    continue;
                }

                var directory = Path.GetDirectoryName(textureFiles[i]);
                var file = Path.GetFileName(textureFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                string text;
                TextureMetadata metadata;

                try
                {
                    text = File.ReadAllText(textureFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<TextureMetadata>(text);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    continue;
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

                var parameters = $"-t {metadata.format} -q {metadata.quality.ToString().ToLowerInvariant()} --max {metadata.maxSize} --as dds";

                if (metadata.isLinear)
                {
                    parameters += " --linear";
                }

                if (metadata.useMipmaps)
                {
                    parameters += " --mips";
                }

                switch (metadata.type)
                {
                    case TextureType.NormalMap:

                        parameters += " --normalmap";

                        break;
                }

                if (metadata.premultiplyAlpha)
                {
                    parameters += " --pma";
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                var outputFileTemp = $"{outputFile}_temp";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shadercPath,
                        Arguments = $"-f \"{textureFiles[i].Replace(".meta", "")}\" -o \"{outputFileTemp}\" {parameters} --validate",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                var result = "";

                while (!process.StandardOutput.EndOfStream)
                {
                    result += $"{process.StandardOutput.ReadLine()}\n";
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\tError:\n\t{result}\n");

                    Environment.Exit(1);

                    return;
                }

                try
                {
                    var texture = new SerializableTexture()
                    {
                        metadata = metadata,
                        data = File.ReadAllBytes(outputFileTemp),
                    };

                    var header = new SerializableTextureHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(texture));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked texture: {e}");

                    Environment.Exit(1);

                    return;
                }
                finally
                {
                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
