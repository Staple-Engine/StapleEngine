using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using Staple.Tooling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    private static void ProcessAssets(AppPlatform platform, string inputPath, string outputPath)
    {
        var assetFiles = new List<string>();

        try
        {
            assetFiles.AddRange(Directory.GetFiles(inputPath, $"*.{AssetSerialization.AssetExtension}", SearchOption.AllDirectories));
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"Processing {assetFiles.Count} assets...");

        for (var i = 0; i < assetFiles.Count; i++)
        {
            var assetFileName = assetFiles[i];

            //Console.WriteLine($"\t{assetFileName}");

            try
            {
                if (File.Exists(assetFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {assetFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {assetFileName} doesn't exist");

                continue;
            }

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(assetFileName));
            var file = Path.GetFileName(assetFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            WorkScheduler.Main.Dispatch(Path.GetFileName(assetFileName.Replace(".meta", "")), () =>
            {
                //Console.WriteLine($"\t\t -> {outputFile}");

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
                    var json = File.ReadAllText(assetFileName);

                    var assetData = JsonConvert.DeserializeObject<SerializableStapleAsset>(json);

                    Utilities.ExpandSerializedAsset(assetData);

                    var header = new SerializableStapleAssetHeader();

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(assetData));

                    File.WriteAllBytes(outputFile, encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save asset: {e}");
                }
            });
        }
    }
}
