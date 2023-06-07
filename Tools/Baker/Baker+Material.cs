using MessagePack;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessMaterials(string inputPath, string outputPath)
        {
            var materialFiles = new List<string>();

            try
            {
                materialFiles.AddRange(Directory.GetFiles(inputPath, $"*.mat", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }

            Console.WriteLine($"Processing {materialFiles.Count} materials...");

            for (var i = 0; i < materialFiles.Count; i++)
            {
                Console.WriteLine($"\t{materialFiles[i]}");

                try
                {
                    if (File.Exists(materialFiles[i]) == false)
                    {
                        Console.WriteLine($"\t\tError: {materialFiles[i]} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {materialFiles[i]} doesn't exist");

                    continue;
                }

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(materialFiles[i]));
                var file = Path.GetFileName(materialFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                MaterialMetadata metadata;

                try
                {
                    text = File.ReadAllText(materialFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<MaterialMetadata>(text);
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

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(material));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked material: {e}");
                }
            }
        }
    }
}
