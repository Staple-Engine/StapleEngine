using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Staple.Internal;
using System.Runtime.InteropServices;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    private readonly List<ModuleType> moduleKinds = [];
    private readonly Dictionary<ModuleType, string[]> moduleNames = [];

    public AppSettingsWindow()
    {
        title = "Settings";

        allowDocking = false;

        moduleKinds = Enum.GetValues<ModuleType>().ToList();

        foreach(var pair in StapleEditor.instance.modulesList)
        {
            var names = pair.Value
                .Select(x => x.moduleName)
                .ToList();

            names.Insert(0, "(None)");

            moduleNames.Add(pair.Key, names.ToArray());
        }
    }

    public override void OnGUI()
    {
        base.OnGUI();

        EditorGUI.TreeNode("General", "AppSettings.General", false, () =>
        {
            projectAppSettings.appName = EditorGUI.TextField("App Name", "AppSettings.General.AppName", projectAppSettings.appName ?? "");

            projectAppSettings.companyName = EditorGUI.TextField("Company Name", "AppSettings.General.CompanyName", projectAppSettings.companyName ?? "");

            projectAppSettings.appBundleID = EditorGUI.TextField("App Bundle ID", "AppSettings.General.BundleID", projectAppSettings.appBundleID ?? "");

            projectAppSettings.appDisplayVersion = EditorGUI.TextField("App Display Version", "AppSettings.General.DisplayVersion", projectAppSettings.appDisplayVersion ?? "");

            projectAppSettings.appVersion = EditorGUI.IntField("App Version ID", "AppSettings.General.VersionID", projectAppSettings.appVersion);

            projectAppSettings.profilingMode = EditorGUI.EnumDropdown("Profiling", "AppSettings.General.Profiling", projectAppSettings.profilingMode);
        }, null);

        EditorGUI.TreeNode("Timing", "AppSettings.Timing", false, () =>
        {
            projectAppSettings.fixedTimeFrameRate = EditorGUI.IntField("Fixed Time Frame Rate", "AppSettings.Timing.FixedTimeFrameRate",
                projectAppSettings.fixedTimeFrameRate);

            if (projectAppSettings.fixedTimeFrameRate <= 0)
            {
                projectAppSettings.fixedTimeFrameRate = 1;
            }

            projectAppSettings.maximumFixedTimestepTime = EditorGUI.FloatField("Maximum time spent on fixed timesteps", "AppSettings.Timing.MaximumTimestep",
                projectAppSettings.maximumFixedTimestepTime);

            if (projectAppSettings.maximumFixedTimestepTime <= 0)
            {
                projectAppSettings.maximumFixedTimestepTime = 0.1f;
            }
        }, null);

        EditorGUI.TreeNode("Physics", "AppSettings.Physics", false, () =>
        {
            projectAppSettings.physicsFrameRate = EditorGUI.IntField("Physics Frame Rate", "AppSettings.Physics.FrameRate", projectAppSettings.physicsFrameRate);

            if (projectAppSettings.physicsFrameRate <= 0)
            {
                projectAppSettings.physicsFrameRate = 1;
            }
        }, null);

        EditorGUI.TreeNode("Layers", "AppSettings.Layers", false, () =>
        {
            void Handle(List<string> layers)
            {
                EditorGUI.SameLine();

                EditorGUI.Button("+", "AppSettings.Layers.Add", () =>
                {
                    layers.Add("Layer");
                });

                for (var i = 0; i < layers.Count; i++)
                {
                    layers[i] = EditorGUI.TextField($"Layer {i + 1}", $"AppSettings.Layers{i}", layers[i]);

                    //Can't remove default layer
                    if (i > 1)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Up", $"AppSettings.Layers{i}.Up", () =>
                        {
                            (layers[i], layers[i - 1]) = (layers[i - 1], layers[i]);
                        });
                    }

                    if (i > 0 && i + 1 < layers.Count)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Down", $"AppSettings.Layers{i}.Down", () =>
                        {
                            (layers[i], layers[i + 1]) = (layers[i + 1], layers[i]);
                        });
                    }

                    //Can't remove default layer
                    if (i > 0)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("X", $"AppSettings.Layers{i}.Remove", () =>
                        {
                            layers.RemoveAt(i);
                        });
                    }
                }

                LayerMask.SetLayers(CollectionsMarshal.AsSpan(projectAppSettings.layers), CollectionsMarshal.AsSpan(projectAppSettings.sortingLayers));

                StapleEditor.instance.AddEditorLayers();
            }

            EditorGUI.Label("Layers");

            Handle(projectAppSettings.layers);

            EditorGUI.Label("Sorting Layers");

            Handle(projectAppSettings.sortingLayers);
        }, null);

        EditorGUI.TreeNode("Modules", "AppSettings.Modules", false, () =>
        {
            foreach (var kind in moduleKinds)
            {
                if (StapleEditor.instance.modulesList.TryGetValue(kind, out var modules))
                {
                    projectAppSettings.usedModules.TryGetValue(kind, out var localName);

                    var index = EditorGUI.Dropdown(kind.ToString(), $"AppSettings.Modules{kind}", moduleNames[kind], modules.FindIndex(x => x.moduleName == localName) + 1);

                    if (index > 0 && index <= modules.Count)
                    {
                        projectAppSettings.usedModules.AddOrSetKey(kind, modules[index - 1].moduleName);
                    }
                    else
                    {
                        projectAppSettings.usedModules.AddOrSetKey(kind, "");
                    }
                }
            }
        }, null);

        EditorGUI.TreeNode("Lighting", $"AppSettings.Lighting", false, () =>
        {
            {
                var current = projectAppSettings.enableLighting;

                projectAppSettings.enableLighting = EditorGUI.Toggle("Enable Lighting", "AppSettings.Lighting.EnableLighting", current);

                if(projectAppSettings.enableLighting != current)
                {
                    LightSystem.Enabled = projectAppSettings.enableLighting;
                }
            }

            {
                var current = projectAppSettings.ambientLight;

                projectAppSettings.ambientLight = EditorGUI.ColorField("Ambient Color", "AppSettings.Lighting.AmbientColor", projectAppSettings.ambientLight);

                if (projectAppSettings.ambientLight != current)
                {
                    AppSettings.Current.ambientLight = projectAppSettings.ambientLight;
                }
            }
        }, null);

        EditorGUI.TreeNode("Rendering and Presentation", $"AppSettings.Rendering", false, () =>
        {
            projectAppSettings.runInBackground = EditorGUI.Toggle("Run in Background", $"AppSettings.Rendering.Background", projectAppSettings.runInBackground);

            projectAppSettings.multiThreadedRenderer = EditorGUI.Toggle("Multithreaded Renderer (experimental)", $"AppSettings.Rendering.Multithreaded",
                projectAppSettings.multiThreadedRenderer);

            projectAppSettings.allowFullscreenSwitch = EditorGUI.Toggle("Allow fullscreen switch", "AppSettings.Rendering.AllowFullscreenSwitch",
                projectAppSettings.allowFullscreenSwitch);

            EditorGUI.TabBar(PlayerBackendManager.BackendNames, "AppSettings.Rendering.Backends", (index) =>
            {
                var backend = PlayerBackendManager.Instance.GetBackend(PlayerBackendManager.BackendNames[index]);

                if (backend.platform == AppPlatform.Windows ||
                    backend.platform == AppPlatform.Linux ||
                    backend.platform == AppPlatform.MacOSX)
                {
                    projectAppSettings.defaultWindowMode = EditorGUI.EnumDropdown("Window Mode *", $"AppSettings.Rendering.Backend{index}.WindowMode",
                        projectAppSettings.defaultWindowMode);

                    projectAppSettings.defaultWindowWidth = EditorGUI.IntField("Window Width *", $"AppSettings.Rendering.Backend{index}.WindowWidth",
                        projectAppSettings.defaultWindowWidth);

                    projectAppSettings.defaultWindowHeight = EditorGUI.IntField("Window Height *", $"AppSettings.Rendering.Backend{index}.WindowHeight",
                        projectAppSettings.defaultWindowHeight);
                }
                else if (backend.platform == AppPlatform.Android ||
                    backend.platform == AppPlatform.iOS)
                {
                    projectAppSettings.portraitOrientation = EditorGUI.Toggle("Portrait Orientation *", $"AppSettings.Rendering.Backend{index}.Portrait",
                        projectAppSettings.portraitOrientation);

                    projectAppSettings.landscapeOrientation = EditorGUI.Toggle("Landscape Orientation *", $"AppSettings.Rendering.Backend{index}.Landscape",
                        projectAppSettings.landscapeOrientation);

                    if (backend.platform == AppPlatform.Android)
                    {
                        projectAppSettings.androidMinSDK = EditorGUI.IntField("Android Min SDK", $"AppSettings.Rendering.Backend{index}.AndroidSDK",
                            projectAppSettings.androidMinSDK);

                        if (projectAppSettings.androidMinSDK < 26)
                        {
                            projectAppSettings.androidMinSDK = 26;
                        }
                    }
                    else if (backend.platform == AppPlatform.iOS)
                    {
                        projectAppSettings.iOSDeploymentTarget = EditorGUI.IntField("iOS Deployment Target", $"AppSettings.Rendering.Backend{index}.iOSDeploymentTarget",
                            projectAppSettings.iOSDeploymentTarget);

                        if (projectAppSettings.iOSDeploymentTarget < 13)
                        {
                            projectAppSettings.iOSDeploymentTarget = 13;
                        }
                    }
                }

                EditorGUI.Label("Renderers");

                if (projectAppSettings.renderers.TryGetValue(backend.platform, out var renderers) == false)
                {
                    renderers = [];

                    projectAppSettings.renderers.Add(backend.platform, renderers);
                }

                for (var i = 0; i < renderers.Count; i++)
                {
                    var result = EditorGUI.EnumDropdown("Renderer", $"AppSettings.Rendering.Renderer{index}{i}", renderers[i], backend.renderers);

                    if (result != renderers[i] && renderers.All(x => x != result))
                    {
                        renderers[i] = result;
                    }

                    EditorGUI.SameLine();

                    EditorGUI.Button("-", $"AppSettings.Rendering.Renderer{index}{i}.Remove", () =>
                    {
                        renderers.RemoveAt(i);
                    });
                }

                EditorGUI.Button("+", $"AppSettings.Rendering.Renderer{index}.Add", () =>
                {
                    renderers.Add(backend.renderers.FirstOrDefault());
                });
            });
        }, null);

        EditorGUI.Label("* - Shared setting between platforms");

        EditorGUI.Button("Apply Changes", "AppSettings.ApplyChanges", () =>
        {
            try
            {
                var json = JsonConvert.SerializeObject(projectAppSettings, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                File.WriteAllText(Path.Combine(basePath, "Settings", "AppSettings.json"), json);
            }
            catch (Exception)
            {
            }
        });
    }
}
