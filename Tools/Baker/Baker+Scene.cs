using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessScenes(AppPlatform platform, string inputPath, string outputPath, bool editorMode)
        {
            var sceneFiles = new List<string>();

            try
            {
                sceneFiles.AddRange(Directory.GetFiles(inputPath, $"*.stsc", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }

            Console.WriteLine($"Processing {sceneFiles.Count} scenes...");

            for (var i = 0; i < sceneFiles.Count; i++)
            {
                //Guid collision fix
                Thread.Sleep(25);

                Console.WriteLine($"\t{sceneFiles[i]}");

                try
                {
                    if (File.Exists(sceneFiles[i]) == false)
                    {
                        Console.WriteLine($"\t\tError: {sceneFiles[i]} doesn't exist");

                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: {sceneFiles[i]} doesn't exist");

                    continue;
                }

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(sceneFiles[i]));
                var file = Path.GetFileName(sceneFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                var guid = Guid.NewGuid().ToString();

                try
                {
                    var meta = Path.ChangeExtension(sceneFiles[i], ".stsc.meta");

                    guid = File.ReadAllText(meta);

                    Console.WriteLine($"\t\tReusing guid {guid}");
                }
                catch (Exception)
                {
                }

                Console.WriteLine($"\t\t -> {outputFile}");

                string text;
                List<SceneObject> metadata;

                try
                {
                    text = File.ReadAllText(sceneFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<List<SceneObject>>(text);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    continue;
                }

                if(metadata == null)
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

                foreach(var item in metadata)
                {
                    if(item == null || item.components == null)
                    {
                        continue;
                    }

                    foreach(var component in item.components)
                    {
                        if(component == null || component.data == null)
                        {
                            continue;
                        }

                        foreach (var pair in component.data)
                        {
                            if (pair.Value != null)
                            {
                                if (pair.Value.GetType() == typeof(string))
                                {
                                    component.parameters.Add(new SceneComponentParameter()
                                    {
                                        name = pair.Key,
                                        type = SceneComponentParameterType.String,
                                        stringValue = (string)pair.Value,
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

                                    if(r != null && g != null && b != null && a != null)
                                    {
                                        component.parameters.Add(new SceneComponentParameter()
                                        {
                                            name = pair.Key,
                                            type = SceneComponentParameterType.String,
                                            stringValue = $"#{((byte)r.Value).ToString("X2")}{((byte)g.Value).ToString("X2")}{((byte)b.Value).ToString("X2")}{((byte)a.Value).ToString("X2")}",
                                        });

                                        continue;
                                    }

                                    var x = o.GetValue("x")?.Value<int?>();
                                    var y = o.GetValue("y")?.Value<int?>();
                                    var z = o.GetValue("z")?.Value<int?>();
                                    var w = o.GetValue("w")?.Value<int?>();

                                    if(x != null && y != null && z != null && w != null)
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
    }
}
