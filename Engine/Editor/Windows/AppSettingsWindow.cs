using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Staple.Internal;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    private List<ModuleType> moduleKinds = new();
    private Dictionary<ModuleType, string[]> moduleNames = new();

    public AppSettingsWindow()
    {
        allowDocking = false;

        moduleKinds = Enum.GetValues<ModuleType>().ToList();

        foreach(var pair in StapleEditor.instance.modulesList)
        {
            moduleNames.Add(pair.Key, pair.Value
                .Select(x => x.moduleName)
                .ToArray());
        }
    }

    public override void OnGUI()
    {
        base.OnGUI();

        EditorGUI.TreeNode("General", false, true, () =>
        {
            projectAppSettings.appName = EditorGUI.TextField("App Name", projectAppSettings.appName ?? "");

            projectAppSettings.companyName = EditorGUI.TextField("Company Name", projectAppSettings.companyName ?? "");

            projectAppSettings.appBundleID = EditorGUI.TextField("App Bundle ID", projectAppSettings.appBundleID ?? "");

            projectAppSettings.appDisplayVersion = EditorGUI.TextField("App Display Version", projectAppSettings.appDisplayVersion ?? "");

            projectAppSettings.appVersion = EditorGUI.IntField("App Version ID", projectAppSettings.appVersion);
        });

        EditorGUI.TreeNode("Timing", false, true, () =>
        {
            projectAppSettings.fixedTimeFrameRate = EditorGUI.IntField("Fixed Time Frame Rate", projectAppSettings.fixedTimeFrameRate);

            if (projectAppSettings.fixedTimeFrameRate <= 0)
            {
                projectAppSettings.fixedTimeFrameRate = 1;
            }

            projectAppSettings.maximumFixedTimestepTime = EditorGUI.FloatField("Maximum time spent on fixed timesteps", projectAppSettings.maximumFixedTimestepTime);

            if (projectAppSettings.maximumFixedTimestepTime <= 0)
            {
                projectAppSettings.maximumFixedTimestepTime = 0.1f;
            }
        });

        EditorGUI.TreeNode("Physics", false, true, () =>
        {
            projectAppSettings.physicsFrameRate = EditorGUI.IntField("Physics Frame Rate", projectAppSettings.physicsFrameRate);

            if (projectAppSettings.physicsFrameRate <= 0)
            {
                projectAppSettings.physicsFrameRate = 1;
            }
        });

        EditorGUI.TreeNode("Layers", false, true, () =>
        {
            void Handle(List<string> layers)
            {
                EditorGUI.SameLine();

                EditorGUI.Button("+", () =>
                {
                    layers.Add("Layer");
                });

                for (var i = 0; i < layers.Count; i++)
                {
                    layers[i] = EditorGUI.TextField($"Layer {i + 1}", layers[i]);

                    //Can't remove default layer
                    if (i > 1)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Up", () =>
                        {
                            (layers[i], layers[i - 1]) = (layers[i - 1], layers[i]);
                        });
                    }

                    if (i > 0 && i + 1 < layers.Count)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Down", () =>
                        {
                            (layers[i], layers[i + 1]) = (layers[i + 1], layers[i]);
                        });
                    }

                    //Can't remove default layer
                    if (i > 0)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("X", () =>
                        {
                            layers.RemoveAt(i);
                        });
                    }
                }

                LayerMask.AllLayers = new(projectAppSettings.layers);
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;

                StapleEditor.instance.AddEditorLayers();
            }

            EditorGUI.Label("Layers");

            Handle(projectAppSettings.layers);

            EditorGUI.Label("Sorting Layers");

            Handle(projectAppSettings.sortingLayers);
        });

        EditorGUI.TreeNode("Rendering and Presentation", false, true, () =>
        {
            projectAppSettings.runInBackground = EditorGUI.Toggle("Run in Background", projectAppSettings.runInBackground);

            projectAppSettings.multiThreadedRenderer = EditorGUI.Toggle("Multithreaded Renderer (experimental)", projectAppSettings.multiThreadedRenderer);

            EditorGUI.TabBar(PlayerBackendManager.BackendNames, (index) =>
            {
                var backend = PlayerBackendManager.Instance.GetBackend(PlayerBackendManager.BackendNames[index]);

                if (backend.platform == AppPlatform.Windows ||
                    backend.platform == AppPlatform.Linux ||
                    backend.platform == AppPlatform.MacOSX)
                {
                    projectAppSettings.defaultWindowMode = EditorGUI.EnumDropdown("Window Mode *", projectAppSettings.defaultWindowMode);

                    projectAppSettings.defaultWindowWidth = EditorGUI.IntField("Window Width *", projectAppSettings.defaultWindowWidth);

                    projectAppSettings.defaultWindowHeight = EditorGUI.IntField("Window Height *", projectAppSettings.defaultWindowHeight);
                }
                else if (backend.platform == AppPlatform.Android ||
                    backend.platform == AppPlatform.iOS)
                {
                    projectAppSettings.portraitOrientation = EditorGUI.Toggle("Portrait Orientation *", projectAppSettings.portraitOrientation);

                    projectAppSettings.landscapeOrientation = EditorGUI.Toggle("Landscape Orientation *", projectAppSettings.landscapeOrientation);

                    if (backend.platform == AppPlatform.Android)
                    {
                        projectAppSettings.androidMinSDK = EditorGUI.IntField("Android Min SDK", projectAppSettings.androidMinSDK);

                        if (projectAppSettings.androidMinSDK < 26)
                        {
                            projectAppSettings.androidMinSDK = 26;
                        }
                    }
                    else if (backend.platform == AppPlatform.iOS)
                    {
                        projectAppSettings.iOSDeploymentTarget = EditorGUI.IntField("iOS Deployment Target", projectAppSettings.iOSDeploymentTarget);

                        if (projectAppSettings.iOSDeploymentTarget < 13)
                        {
                            projectAppSettings.iOSDeploymentTarget = 13;
                        }
                    }
                }

                EditorGUI.Label("Renderers");

                if (projectAppSettings.renderers.TryGetValue(backend.platform, out var renderers) == false)
                {
                    renderers = new();

                    projectAppSettings.renderers.Add(backend.platform, renderers);
                }

                for (var i = 0; i < renderers.Count; i++)
                {
                    var result = EditorGUI.EnumDropdown("Renderer", renderers[i], backend.renderers);

                    if (result != renderers[i] && renderers.All(x => x != result))
                    {
                        renderers[i] = result;
                    }

                    EditorGUI.SameLine();

                    EditorGUI.Button("-", () =>
                    {
                        renderers.RemoveAt(i);
                    });
                }

                EditorGUI.Button("+", () =>
                {
                    renderers.Add(backend.renderers.FirstOrDefault());
                });
            });
        });

        EditorGUI.Label("* - Shared setting between platforms");

        EditorGUI.TreeNode("Plugins", false, true, () =>
        {
            foreach(var kind in moduleKinds)
            {
                if (StapleEditor.instance.modulesList.TryGetValue(kind, out var modules))
                {
                    projectAppSettings.usedModules.TryGetValue(kind, out var localName);

                    var index = EditorGUI.Dropdown(kind.ToString(), moduleNames[kind], modules.FindIndex(x => x.moduleName == localName));

                    if(index >= 0 && index < modules.Count)
                    {
                        projectAppSettings.usedModules.AddOrSetKey(kind, modules[index].moduleName);
                    }
                }
            }
        });

        EditorGUI.Button("Apply Changes", () =>
        {
            try
            {
                var json = JsonConvert.SerializeObject(projectAppSettings, Formatting.Indented, new JsonSerializerSettings()
                {
                    Converters =
                    {
                        new StringEnumConverter(),
                    }
                });

                File.WriteAllText(Path.Combine(basePath, "Settings", "AppSettings.json"), json);
            }
            catch (Exception)
            {
            }
        });

        EditorGUI.SameLine();

        EditorGUI.Button("Close", () =>
        {
            Close();
        });
    }
}
