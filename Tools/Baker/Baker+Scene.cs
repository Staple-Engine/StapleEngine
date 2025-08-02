using MessagePack;
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
    private static void ProcessScenes(AppPlatform platform, string inputPath, string outputPath, string sceneListOutputPath, bool editorMode)
    {
        var sceneFiles = new List<string>();

        try
        {
            sceneFiles.AddRange(Directory.GetFiles(inputPath, $"*.{AssetSerialization.SceneExtension}", SearchOption.AllDirectories));
        }
        catch (Exception)
        {
        }

        Console.WriteLine($"Processing {sceneFiles.Count} scenes...");

        for (var i = 0; i < sceneFiles.Count; i++)
        {
            var sceneFileName = sceneFiles[i];

            //Console.WriteLine($"\t{sceneFileName}");

            try
            {
                if (File.Exists(sceneFileName) == false)
                {
                    Console.WriteLine($"\t\tError: {sceneFileName} doesn't exist");

                    continue;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: {sceneFileName} doesn't exist");

                continue;
            }

            var guid = FindGuid<Scene>(sceneFileName);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(sceneFileName));
            var file = Path.GetFileName(sceneFileName);
            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(sceneFileName, outputFile) == false &&
                ShouldProcessFile(sceneFileName.Replace(".meta", ""), outputFile.Replace(".meta", "")) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(sceneFileName.Replace(".meta", "")), () =>
            {
                //Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                List<SceneObject> metadata;

                try
                {
                    text = File.ReadAllText(sceneFileName);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    return;
                }

                try
                {
                    metadata = JsonSerializer.Deserialize(text, SceneObjectSerializationContext.Default.ListSceneObject);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

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

                foreach (var item in metadata)
                {
                    if (item == null || item.components == null)
                    {
                        continue;
                    }

                    foreach (var component in item.components)
                    {
                        if (component == null || component.data == null)
                        {
                            continue;
                        }

                        ConvertComponentDataIntoParameters(component);
                    }
                }

                try
                {
                    var scene = new SerializableScene()
                    {
                        objects = metadata,
                        guid = guid,
                    };

                    var header = new SerializableSceneHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(scene));

                    writer.Write(encoded.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked scene: {e}");
                }
            });
        }

        string sceneListText;

        try
        {
            var p = Path.Combine(inputPath, "SceneList.json");

            if(editorMode)
            {
                p = Path.Combine(inputPath, "..", "Settings", "SceneList.json");
            }

            sceneListText = File.ReadAllText(p);
        }
        catch(Exception)
        {
            Console.WriteLine($"\t\tError: Failed to read scene list");

            return;
        }

        if ((sceneListText?.Length ?? 0) > 0)
        {
            List<string> sceneList;

            try
            {
                sceneList = JsonSerializer.Deserialize(sceneListText, SceneListSerializationContext.Default.ListString);
            }
            catch(Exception)
            {
                Console.WriteLine($"\t\tError: Failed to load scene list");

                return;
            }

            if(sceneList != null)
            {
                var outputFile = Path.Combine(sceneListOutputPath, "SceneList");

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                var header = new SceneListHeader();

                var sceneListObject = new SceneList()
                {
                    scenes = sceneList,
                };

                using var stream = File.OpenWrite(outputFile);
                using var writer = new BinaryWriter(stream);

                var encoded = MessagePackSerializer.Serialize(header)
                    .Concat(MessagePackSerializer.Serialize(sceneListObject));

                writer.Write(encoded.ToArray());

                Console.WriteLine($"\tProcessed scene list");
            }
        }
    }

    public static void ConvertComponentDataIntoParameters(SceneComponent component)
    {
        if (component?.data == null)
        {
            return;
        }

        foreach (var pair in component.data)
        {
            if (pair.Value != null)
            {
                if(pair.Value is JsonElement element)
                {
                    component.parameters.Add(pair.Key, element.GetRawValue());
                }
                else
                {
                    component.parameters.Add(pair.Key, Utilities.ExpandNewtonsoftObject(pair.Value));
                }
            }
        }
    }
}
