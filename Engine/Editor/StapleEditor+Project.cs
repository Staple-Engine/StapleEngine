using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;
using System.Text.RegularExpressions;

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

        public void RefreshAssets(bool updateProject)
        {
            RefreshStaging(currentPlatform, updateProject);
        }

        public void RefreshStaging(AppPlatform platform, bool updateProject = true)
        {
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
            }
        }

        private static Regex cachePathRegex = CachePathRegex();

        public static string GetAssetPathFromCache(string cachePath)
        {
            var matches = cachePathRegex.Matches(cachePath);

            if (matches.Count > 0)
            {
                return Path.Combine("Assets", cachePath.Substring(matches[0].Value.Length));
            }

            return cachePath;
        }

        [GeneratedRegex("(.*?)(\\\\|\\/)Cache(\\\\|\\/)Staging(\\\\|\\/)(.*?)(\\\\|\\/)")]
        private static partial Regex CachePathRegex();
    }
}