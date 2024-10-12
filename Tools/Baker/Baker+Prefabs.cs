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
    private static void ProcessPrefabs(AppPlatform platform, string inputPath, string outputPath)
    {
        var prefabFiles = new List<string>();

        try
        {
            prefabFiles.AddRange(Directory.GetFiles(inputPath, $"*.stpr", SearchOption.AllDirectories));
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"Processing {prefabFiles.Count} prefabs...");

        for (var i = 0; i < prefabFiles.Count; i++)
        {
            var prefabFileName = prefabFiles[i];

            Console.WriteLine($"\t{prefabFileName}");

            try
            {
                if (File.Exists(prefabFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {prefabFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {prefabFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Prefab>(prefabFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(prefabFileName));
            var file = Path.GetFileName(prefabFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            WorkScheduler.Dispatch(Path.GetFileName(prefabFileName.Replace(".meta", "")), () =>
            {
                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                SerializablePrefab prefab;

                try
                {
                    text = File.ReadAllText(prefabFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    prefab = JsonConvert.DeserializeObject<SerializablePrefab>(text);

                    prefab.guid = guid;
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Prefab is corrupted");

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

                foreach(var component in prefab.mainObject.components)
                {
                    ConvertComponentDataIntoParameters(component);
                }

                foreach (var sceneObject in prefab.children)
                {
                    foreach(var component in sceneObject.components)
                    {
                        if(component == null || component.data == null)
                        {
                            continue;
                        }

                        ConvertComponentDataIntoParameters(component);
                    }
                }

                try
                {
                    var header = new SerializablePrefabHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(prefab));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked prefab: {e}");
                }
            });
        }
    }
}
