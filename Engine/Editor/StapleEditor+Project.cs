using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;
using System.Reflection;
using MessagePack;
using System.Linq;
using NfdSharp;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private void ImGuiNewProject()
    {
        var result = Nfd.PickFolder("", out var projectPath);

        if (result == Nfd.NfdResult.NFD_OKAY)
        {
            CreateProject(projectPath);
            LoadProject(projectPath);
        }
    }

    private void ImGuiOpenProject()
    {
        var result = Nfd.PickFolder("", out var projectPath);

        if (result == Nfd.NfdResult.NFD_OKAY)
        {
            LoadProject(projectPath);
        }
    }

    public void LoadProject(string path)
    {
        try
        {
            var json = File.ReadAllText(Path.Combine(path, "ProjectInfo.json"));

            var projectInfo = JsonConvert.DeserializeObject<ProjectInfo>(json);

            if (projectInfo.stapleVersion != StapleVersion)
            {
                return;
            }
        }
        catch (Exception)
        {
            return;
        }

        basePath =
            ThumbnailCache.basePath =
            csProjManager.basePath =
            projectBrowser.basePath =
            Path.GetFullPath(path);

        AssetDatabase.assetDirectories.Clear();
        AssetDatabase.assetDirectories.Add(Path.Combine(basePath, "Assets"));

        csProjManager.stapleBasePath = StapleBasePath;

        Log.Info($"Project Path: {basePath}");

        projectBrowser.UpdateProjectBrowserNodes();

        projectBrowser.CreateMissingMetaFiles();

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

        var lastSession = GetLastSession();

        if(lastSession != null)
        {
            currentPlatform = lastSession.currentPlatform;
            lastOpenScene = lastSession.lastOpenScene;
            lastPickedBuildDirectories = lastSession.lastPickedBuildDirectories;
        }

        if(fileSystemWatcher != null)
        {
            fileSystemWatcher.Dispose();

            fileSystemWatcher = null;
        }

        fileSystemWatcher = new FileSystemWatcher(Path.Combine(basePath, "Assets"));

        fileSystemWatcher.Changed += (_, _) =>
        {
            needsGameRecompile = true;
        };

        fileSystemWatcher.EnableRaisingEvents = true;

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

        lastProjects.lastOpenProject = path;

        var target = lastProjects.items.FirstOrDefault(x => x.path == path);

        if (target != null)
        {
            target.date = DateTime.Now;
        }
        else
        {
            lastProjects.items.Add(new LastProjectItem()
            {
                name = Path.GetFileName(path),
                path = path,
                date = DateTime.Now,
            });
        }

        SaveLastProjects();

        window.Title = $"Staple Editor - {Path.GetFileName(path)}";
    }

    public void RefreshAssets(bool updateProject)
    {
        RefreshStaging(currentPlatform, updateProject);

        projectBrowser.UpdateProjectBrowserNodes();
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
                RedirectStandardError = true,
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

                var all = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if(all != null && all.Length > 0)
                {
                    Log.Info(all);
                }

                if(error != null && error.Length > 0)
                {
                    Log.Error(error);
                }
            }

            foreach(var pair in ResourceManager.instance.cachedMeshes)
            {
                pair.Value.Destroy();
            }

            ResourceManager.instance.cachedMeshAssets.Clear();
            ResourceManager.instance.cachedMeshes.Clear();

            if (World.Current != null)
            {
                Scene.IterateEntities((entity) =>
                {
                    entity.IterateComponents((ref IComponent component) =>
                    {
                        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                        foreach(var field in fields)
                        {
                            if(field.FieldType == typeof(Texture))
                            {
                                var value = (Texture)field.GetValue(component);

                                if(value != null && value.Disposed && (value.Guid?.Length ?? 0) > 0)
                                {
                                    field.SetValue(component, ResourceManager.instance.LoadTexture(value.Guid));
                                }
                            }
                            else if(field.FieldType == typeof(Mesh))
                            {
                                var value = (Mesh)field.GetValue(component);

                                if(value != null && (value.Guid?.Length ?? 0) > 0)
                                {
                                    field.SetValue(component, ResourceManager.instance.LoadMesh(value.Guid));
                                }
                            }
                        }
                    });
                });
            }

            AssetDatabase.Reload();
            projectBrowser.UpdateProjectBrowserNodes();
        }
    }

    public static bool SaveAsset(string assetPath, IStapleAsset assetInstance)
    {
        var existed = false;

        try
        {
            existed = File.Exists(assetPath);
        }
        catch(Exception)
        {
        }

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
        catch(Exception e)
        {
            Log.Debug($"Failed to save asset: {e}");

            if(existed == false)
            {
                try
                {
                    File.Delete(assetPath);
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        return true;
    }
}