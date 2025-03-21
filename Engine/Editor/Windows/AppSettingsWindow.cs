﻿using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Staple.Internal;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    private readonly List<ModuleType> moduleKinds = [];
    private readonly string[] moduleKindStrings = [];
    private readonly Dictionary<ModuleType, string[]> moduleNames = [];

    private static readonly string GeneralKey = "General";
    private static readonly string TimingKey = "Timing";
    private static readonly string PhysicsKey = "Physics";
    private static readonly string LayersKey = "Layers";
    private static readonly string ModulesKey = "Modules";
    private static readonly string LightingKey = "Lighting";
    private static readonly string RenderingKey = "Rendering and Presentation";

    private readonly string[] sections =
    [
        GeneralKey,
        TimingKey,
        PhysicsKey,
        LayersKey,
        ModulesKey,
        LightingKey,
        RenderingKey,
    ];

    public AppSettingsWindow()
    {
        title = "Settings";

        windowFlags = EditorWindowFlags.Resizable;

        moduleKinds = Enum.GetValues<ModuleType>().ToList();
        moduleKindStrings = moduleKinds.Select(x => x.ToString().ExpandCamelCaseName()).ToArray();

        foreach(var pair in StapleEditor.instance.modulesList)
        {
            var names = pair.Value
                .Select(x => x.moduleName)
                .ToList();

            names.Insert(0, "(None)");

            moduleNames.Add(pair.Key, names.ToArray());
        }
    }

    private void General()
    {
        projectAppSettings.appName = EditorGUI.TextField("App Name", "AppSettings.General.AppName", projectAppSettings.appName ?? "");

        projectAppSettings.companyName = EditorGUI.TextField("Company Name", "AppSettings.General.CompanyName", projectAppSettings.companyName ?? "");

        projectAppSettings.appBundleID = EditorGUI.TextField("App Bundle ID", "AppSettings.General.BundleID", projectAppSettings.appBundleID ?? "");

        projectAppSettings.appDisplayVersion = EditorGUI.TextField("App Display Version", "AppSettings.General.DisplayVersion", projectAppSettings.appDisplayVersion ?? "");

        projectAppSettings.appVersion = EditorGUI.IntField("App Version ID", "AppSettings.General.VersionID", projectAppSettings.appVersion);

        projectAppSettings.profilingMode = EditorGUI.EnumDropdown("Profiling", "AppSettings.General.Profiling", projectAppSettings.profilingMode);
    }

    private void Timing()
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
    }

    private void Physics()
    {
        projectAppSettings.physicsFrameRate = EditorGUI.IntField("Physics Frame Rate", "AppSettings.Physics.FrameRate", projectAppSettings.physicsFrameRate);

        if (projectAppSettings.physicsFrameRate <= 0)
        {
            projectAppSettings.physicsFrameRate = 1;
        }
    }

    private void Layers()
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
    }

    private void Modules()
    {
        EditorGUI.TabBar(moduleKindStrings, "AppSettings.Modules.TabBar", (tabIndex) =>
        {
            var key = moduleKinds[tabIndex];

            if (StapleEditor.instance.modulesList.TryGetValue(key, out var modules))
            {
                if (ImGui.BeginTable("AppSettings.Modules.List", 3))
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);

                    EditorGUI.Label("Enabled");

                    ImGui.TableSetColumnIndex(1);

                    EditorGUI.Label("Name");

                    for (var i = 0; i < modules.Count; i++)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);

                        var moduleName = modules[i].moduleName;

                        var had = projectAppSettings.usedModules.Contains(moduleName);

                        var shouldHave = EditorGUI.Toggle("", $"AppSettings.Modules.List.Check{i}", had);

                        if (shouldHave != had)
                        {
                            if (shouldHave)
                            {
                                if (ModuleInitializer.IsModuleTypeUnique(key))
                                {
                                    foreach (var m in modules)
                                    {
                                        projectAppSettings.usedModules.Remove(m.moduleName);
                                    }
                                }

                                projectAppSettings.usedModules.Add(moduleName);
                            }
                            else
                            {
                                projectAppSettings.usedModules.Remove(moduleName);
                            }
                        }

                        ImGui.TableSetColumnIndex(1);

                        EditorGUI.Label(moduleName);
                    }

                    ImGui.EndTable();
                }
            }
        });
    }

    private void Lighting()
    {
        {
            var current = projectAppSettings.enableLighting;

            projectAppSettings.enableLighting = EditorGUI.Toggle("Enable Lighting", "AppSettings.Lighting.EnableLighting", current);

            if (projectAppSettings.enableLighting != current)
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
    }

    private void Rendering()
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
    }

    public override void OnGUI()
    {
        EditorGUI.Label("* - Shared setting between platforms");

        EditorGUI.TabBar(sections, "AppSettings.Sections", (tabIndex) =>
        {
            var key = sections[tabIndex];

            switch(key)
            {
                case string str when str == GeneralKey:

                    General();

                    break;

                case string str when str == TimingKey:

                    Timing();

                    break;

                case string str when str == PhysicsKey:

                    Physics();

                    break;

                case string str when str == LayersKey:

                    Layers();

                    break;


                case string str when str == ModulesKey:

                    Modules();

                    break;

                case string str when str == LightingKey:

                    Lighting();

                    break;

                case string str when str == RenderingKey:

                    Rendering();

                    break;
            }
        });

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
