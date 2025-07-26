using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using Staple.Tooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Baker;

static partial class Program
{
    private static void ProcessTextAssets(AppPlatform platform, string inputPath, string outputPath)
    {
        var textFiles = new List<string>();

        try
        {
            foreach(var extension in AssetSerialization.TextExtensions)
            {
                textFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
            }
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"Processing {textFiles.Count} text assets...");

        for (var i = 0; i < textFiles.Count; i++)
        {
            var textFileName = textFiles[i];

            try
            {
                if (File.Exists(textFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {textFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {textFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<TextAsset>(textFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(textFileName));
            var file = Path.GetFileName(textFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(textFileName, outputFile) == false &&
                ShouldProcessFile(textFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(textFileName.Replace(".meta", "")), () =>
            {
                var inputFile = textFileName.Replace(".meta", "");

                //Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                TextAssetMetadata metadata;

                try
                {
                    text = File.ReadAllText(textFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<TextAssetMetadata>(text);
                    metadata.guid = guid;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted {e}");

                    return;
                }

                if (metadata == null)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

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
                    var data = File.ReadAllBytes(inputFile);

                    var asset = new SerializableTextAsset()
                    {
                        metadata = metadata,
                        data = data,
                    };

                    var header = new SerializableTextAssetHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(asset));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to process text asset: {e}");
                }
            });
        }
    }
}
