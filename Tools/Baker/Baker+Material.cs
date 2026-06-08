using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    private static void ProcessMaterials(AppPlatform platform, string inputPath, string outputPath)
    {
        var materialFiles = new List<string>();

        try
        {
            materialFiles.AddRange(Directory.GetFiles(inputPath, $"*.{AssetSerialization.MaterialExtension}", SearchOption.AllDirectories));
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"Processing {materialFiles.Count} materials...");

        for (var i = 0; i < materialFiles.Count; i++)
        {
            var materialFileName = materialFiles[i];

            //Console.WriteLine($"\t{materialFileName}");

            try
            {
                if (File.Exists(materialFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {materialFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {materialFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Material>(materialFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(materialFileName));
            var file = Path.GetFileName(materialFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(materialFileName, outputFile) == false &&
                ShouldProcessFile(materialFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Main.Dispatch(Path.GetFileName(materialFileName.Replace(".meta", "")), () =>
            {
                //Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MaterialMetadata metadata;

                try
                {
                    text = File.ReadAllText(materialFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MaterialMetadata>(text);

                    metadata.guid = guid;
                }
                catch (Exception)
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
                    var material = new SerializableMaterial()
                    {
                        metadata = metadata
                    };

                    var header = new SerializableMaterialHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(material));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked material: {e}");
                }
            });
        }
    }
}
