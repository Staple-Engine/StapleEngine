using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    private static void ProcessScenes(AppPlatform platform, string inputPath, string outputPath, bool editorMode)
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

            Console.WriteLine($"\t{sceneFileName}");

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

            WorkScheduler.Dispatch(Path.GetFileName(sceneFileName.Replace(".meta", "")), () =>
            {
                Console.WriteLine($"\t\t -> {outputFile}");

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
                    metadata = JsonConvert.DeserializeObject<List<SceneObject>>(text);
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

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(scene));

                            writer.Write(encoded.ToArray());
                        }
                    }
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
                sceneList = JsonConvert.DeserializeObject<List<string>>(sceneListText);
            }
            catch(Exception)
            {
                Console.WriteLine($"\t\tError: Failed to load scene list");

                return;
            }

            if(sceneList != null)
            {
                var outputFile = Path.Combine(outputPath, "SceneList");

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

                using (var stream = File.OpenWrite(outputFile))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        var encoded = MessagePackSerializer.Serialize(header)
                            .Concat(MessagePackSerializer.Serialize(sceneListObject));

                        writer.Write(encoded.ToArray());
                    }
                }

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
                if (pair.Value is JArray array)
                {
                    if (array.Count == 0)
                    {
                        continue;
                    }

                    switch (array[0].Type)
                    {
                        case JTokenType.String:

                            var list = new List<string>();

                            foreach (var element in array)
                            {
                                if (element.Type == JTokenType.String)
                                {
                                    list.Add(element.Value<string>());
                                }
                            }

                            component.parameters.Add(new SceneComponentParameter()
                            {
                                name = pair.Key,
                                type = SceneComponentParameterType.Array,
                                arrayType = SceneComponentParameterType.String,
                                arrayValue = list.ToArray(),
                            });

                            break;
                    }
                }
                else if (pair.Value.GetType() == typeof(string))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.String,
                        stringValue = (string)pair.Value,
                    });
                }
                else if (pair.Value.GetType() == typeof(int))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.Int,
                        intValue = (int)pair.Value,
                    });
                }
                else if (pair.Value.GetType() == typeof(float))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.Float,
                        floatValue = (float)pair.Value,
                    });
                }
                else if (pair.Value.GetType() == typeof(long))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.Int,
                        intValue = (int)((long)pair.Value),
                    });
                }
                else if (pair.Value.GetType() == typeof(double))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.Float,
                        floatValue = (float)((double)pair.Value),
                    });
                }
                else if (pair.Value.GetType() == typeof(bool))
                {
                    component.parameters.Add(new SceneComponentParameter()
                    {
                        name = pair.Key,
                        type = SceneComponentParameterType.Bool,
                        boolValue = (bool)pair.Value,
                    });
                }
                else if (pair.Value.GetType() == typeof(JObject))
                {
                    var o = (JObject)pair.Value;

                    var r = o.GetValue("r")?.Value<int?>();
                    var g = o.GetValue("g")?.Value<int?>();
                    var b = o.GetValue("b")?.Value<int?>();
                    var a = o.GetValue("a")?.Value<int?>();

                    if (r != null && g != null && b != null && a != null)
                    {
                        component.parameters.Add(new SceneComponentParameter()
                        {
                            name = pair.Key,
                            type = SceneComponentParameterType.String,
                            stringValue = $"#{((byte)r.Value).ToString("X2")}{((byte)g.Value).ToString("X2")}{((byte)b.Value).ToString("X2")}{((byte)a.Value).ToString("X2")}",
                        });

                        continue;
                    }

                    var x = o.GetValue("x")?.Value<float?>();
                    var y = o.GetValue("y")?.Value<float?>();
                    var z = o.GetValue("z")?.Value<float?>();
                    var w = o.GetValue("w")?.Value<float?>();

                    if (x != null && y != null && z != null && w != null)
                    {
                        component.parameters.Add(new SceneComponentParameter()
                        {
                            name = pair.Key,
                            type = SceneComponentParameterType.Vector4,
                            vector4Value = new Vector4Holder()
                            {
                                x = x.Value,
                                y = y.Value,
                                z = z.Value,
                                w = w.Value,
                            }
                        });

                        continue;
                    }

                    if (x != null && y != null && z != null)
                    {
                        component.parameters.Add(new SceneComponentParameter()
                        {
                            name = pair.Key,
                            type = SceneComponentParameterType.Vector3,
                            vector3Value = new Vector3Holder()
                            {
                                x = x.Value,
                                y = y.Value,
                                z = z.Value,
                            }
                        });

                        continue;
                    }

                    if (x != null && y != null)
                    {
                        component.parameters.Add(new SceneComponentParameter()
                        {
                            name = pair.Key,
                            type = SceneComponentParameterType.Vector2,
                            vector2Value = new Vector2Holder()
                            {
                                x = x.Value,
                                y = y.Value,
                            }
                        });

                        continue;
                    }
                }
            }
        }
    }
}
