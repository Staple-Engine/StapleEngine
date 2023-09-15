using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;
using Newtonsoft.Json.Converters;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void LoadProject(string path)
        {
            basePath = ThumbnailCache.basePath = path;

            Log.Info($"Base Path: {basePath}");

            AssetDatabase.basePath = basePath;

            AssetDatabase.Reload();

            UpdateProjectBrowserNodes();

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Thumbnails"));
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

            void Recursive(List<ProjectBrowserNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if(node.type == ProjectBrowserNodeType.Folder)
                    {
                        {
                            try
                            {
                                if (File.Exists($"{node.path}.meta") == false)
                                {
                                    File.WriteAllText($"{node.path}.meta", Guid.NewGuid().ToString());
                                }
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        Recursive(node.subnodes);
                    }
                    else
                    {
                        switch(node.resourceType)
                        {
                            case ProjectResourceType.Texture:
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new TextureMetadata(), Formatting.Indented, new JsonSerializerSettings()
                                            {
                                                Converters =
                                                {
                                                    new StringEnumConverter(),
                                                }
                                            });

                                            File.WriteAllText($"{node.path}.meta", jsonData);
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;

                            default:

                                if(node.path.EndsWith(".meta") == false)
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            File.WriteAllText($"{node.path}.meta", Guid.NewGuid().ToString());
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;
                        }
                    }
                }
            }

            Recursive(projectBrowserNodes);

            try
            {
                var json = File.ReadAllText(Path.Combine(basePath, "Settings", "AppSettings.json"));

                projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            }
            catch(Exception e)
            {
                Log.Error($"Failed to load project app settings: {e}");

                projectAppSettings = AppSettings.Default;
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
            }

            RefreshStaging(currentPlatform);

            if((lastOpenScene?.Length ?? 0) > 0)
            {
                var scene = ResourceManager.instance.LoadRawSceneFromPath(lastOpenScene);

                if (scene != null)
                {
                    Scene.current = scene;

                    ResetScenePhysics();
                }
            }
        }

        public void RefreshStaging(AppPlatform platform)
        {
            var bakerPath = Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Baker");

            UpdateCSProj(platform);

            if(projectAppSettings == null)
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
            }
        }
    }
}