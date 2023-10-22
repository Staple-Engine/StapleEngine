using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;
using System.Text.RegularExpressions;
using System.Reflection;
using MessagePack;
using System.Linq;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void LoadProject(string path)
        {
            basePath =
                ThumbnailCache.basePath =
                csProjManager.basePath =
                projectBrowser.basePath =
                AssetDatabase.basePath = Path.GetFullPath(path);

            csProjManager.stapleBasePath = StapleBasePath;

            Log.Info($"Project Path: {basePath}");

            projectBrowser.UpdateProjectBrowserNodes();

            projectBrowser.CreateMissingMetaFiles();

            AssetDatabase.Reload();

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging"));
            }
            catch (Exception)
            {
            }

            try
            {
                var json = File.ReadAllText(Path.Combine(basePath, "Settings", "AppSettings.json"));

                projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            }
            catch(Exception e)
            {
                Log.Error($"Failed to load project app settings: {e}");

                projectAppSettings = Staple.AppSettings.Default;
            }

            if(projectAppSettings != null)
            {
                LayerMask.AllLayers = projectAppSettings.layers;
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;

                window.appSettings.fixedTimeFrameRate = projectAppSettings.fixedTimeFrameRate;

                foreach(var pair in projectAppSettings.renderers)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging", pair.Key.ToString()));
                    }
                    catch(Exception)
                    {
                    }
                }
            }

            var lastSession = GetLastSession();

            if(lastSession != null)
            {
                currentPlatform = lastSession.currentPlatform;
                lastOpenScene = lastSession.lastOpenScene;
                lastPickedBuildDirectories = lastSession.lastPickedBuildDirectories;
            }

            RefreshStaging(currentPlatform);

            if ((lastOpenScene?.Length ?? 0) > 0)
            {
                var scene = ResourceManager.instance.LoadRawSceneFromPath(lastOpenScene);

                Scene.SetActiveScene(scene);

                if (scene != null)
                {
                    ResetScenePhysics();
                }
            }
        }

        public void RefreshAssets(bool updateProject)
        {
            RefreshStaging(currentPlatform, updateProject);

            var path = projectBrowser.currentContentNode?.path;

            projectBrowser.UpdateProjectBrowserNodes();

            if(path != null)
            {
                void Recursive(List<ProjectBrowserNode> nodes)
                {
                    foreach(var node in nodes)
                    {
                        if(node.type == ProjectBrowserNodeType.File)
                        {
                            continue;
                        }

                        if(node.path == path)
                        {
                            projectBrowser.currentContentNode = node;

                            return;
                        }

                        Recursive(node.subnodes);
                    }
                }
            }
        }

        public void RefreshStaging(AppPlatform platform, bool updateProject = true)
        {
            if(gameLoadDisabled)
            {
                return;
            }

            var bakerPath = Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Baker");

            if(updateProject)
            {
                UpdateCSProj(platform);
            }

            if (projectAppSettings == null)
            {
                return;
            }

            if(projectAppSettings.renderers.TryGetValue(platform, out var renderers))
            {
                var rendererParameters = new HashSet<string>();

                foreach(var item in renderers)
                {
                    switch(item)
                    {
                        case RendererType.Direct3D11:

                            rendererParameters.Add("-r d3d11");

                            break;

                        case RendererType.Direct3D12:

                            rendererParameters.Add("-r d3d12");

                            break;

                        case RendererType.Metal:

                            rendererParameters.Add("-r metal");

                            break;

                        case RendererType.OpenGL:

                            rendererParameters.Add("-r opengl");

                            break;

                        case RendererType.OpenGLES:

                            rendererParameters.Add("-r opengles");

                            break;

                        case RendererType.Vulkan:

                            rendererParameters.Add("-r spirv");

                            break;
                    }
                }

                var args = $"-i \"{basePath}/Assets\" -o \"{basePath}/Cache/Staging/{platform}\" -platform {platform} -editor {string.Join(" ", rendererParameters)}".Replace("\\", "/");

                var processInfo = new ProcessStartInfo(bakerPath, args)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                var process = new Process
                {
                    StartInfo = processInfo
                };

                if (process.Start())
                {
                    while (process.HasExited == false)
                    {
                        var line = process.StandardOutput.ReadLine();

                        if (line != null)
                        {
                            Log.Info(line);
                        }
                    }

                    for(; ; )
                    {
                        var finalLine = process.StandardOutput.ReadLine();

                        if (finalLine != null)
                        {
                            Log.Info(finalLine);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if(Scene.current?.world != null)
                {
                    Scene.current.world.Iterate((entity) =>
                    {
                        Scene.current.world.IterateComponents(entity, (ref IComponent component) =>
                        {
                            var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                            foreach(var field in fields)
                            {
                                if(field.FieldType == typeof(Texture))
                                {
                                    var value = (Texture)field.GetValue(component);

                                    if(value != null && value.Disposed && (value.path?.Length ?? 0) > 0)
                                    {
                                        field.SetValue(component, ResourceManager.instance.LoadTexture(value.path));
                                    }
                                }
                            }
                        });
                    });
                }
            }
        }

        public static bool SaveAsset(string assetPath, IStapleAsset assetInstance)
        {
            try
            {
                var guidField = assetInstance.GetType().GetField("guid");
                var guid = Guid.NewGuid().ToString();

                if (guidField != null && guidField.FieldType == typeof(string) && guidField.GetValue(assetInstance) != null)
                {
                    guid = (string)guidField.GetValue(assetInstance);
                }

                var serialized = AssetSerialization.Serialize(assetInstance);

                if (serialized != null)
                {
                    serialized.guid = guid;

                    var header = new SerializableStapleAssetHeader();

                    using (var stream = File.OpenWrite(assetPath))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(serialized));

                            writer.Write(encoded.ToArray());
                        }
                    }

                    var holder = new AssetHolder()
                    {
                        guid = guid,
                        typeName = assetInstance.GetType().FullName,
                    };

                    var json = JsonConvert.SerializeObject(holder, Formatting.Indented);

                    File.WriteAllText($"{assetPath}.meta", json);
                }
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }
    }
}