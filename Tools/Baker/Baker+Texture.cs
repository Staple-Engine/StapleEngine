using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessTextures(AppPlatform platform, string texturecPath, string inputPath, string outputPath)
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

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(textureFiles[i]));
                var file = Path.GetFileName(textureFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                if (ShouldProcessFile(textureFiles[i], outputFile) == false &&
                    ShouldProcessFile(textureFiles[i].Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
                {
                    continue;
                }

                Console.WriteLine($"\t\t -> {outputFile}");

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

                var format = metadata.format;
                var quality = metadata.quality;
                var maxSize = metadata.maxSize;
                var premultiplyAlpha = metadata.premultiplyAlpha;

                if(metadata.overrides.TryGetValue(platform, out var overrides))
                {
                    format = overrides.format;
                    quality = overrides.quality;
                    maxSize = overrides.maxSize;
                    premultiplyAlpha = overrides.premultiplyAlpha;
                }

                var parameters = $"-t {format} -q {quality.ToString().ToLowerInvariant()} --max {maxSize} --as dds";

                if (premultiplyAlpha)
                {
                    parameters += " --pma";
                }

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
                        FileName = texturecPath,
                        Arguments = $"-f \"{textureFiles[i].Replace(".meta", "")}\" -o \"{outputFileTemp}\" {parameters} --validate",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                process.WaitForExit(300000);

                var result = "";

                while (!process.StandardOutput.EndOfStream)
                {
                    result += $"{process.StandardOutput.ReadLine()}\n";
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\tError:\n\t{result}\n");

                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    continue;
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

                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }

                    continue;
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
