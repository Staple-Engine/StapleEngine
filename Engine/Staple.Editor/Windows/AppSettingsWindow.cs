using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Staple.Internal;
using System.Runtime.InteropServices;
using Staple.ProjectManagement;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    private static readonly string GeneralKey = "General";
    private static readonly string TimingKey = "Timing";
    private static readonly string PhysicsKey = "Physics";
    private static readonly string LayersKey = "Layers";
    private static readonly string LightingKey = "Lighting";
    private static readonly string RenderingKey = "Rendering and Presentation";

    private readonly string[] sections =
    [
        GeneralKey,
        TimingKey,
        PhysicsKey,
        LayersKey,
        LightingKey,
        RenderingKey,
    ];

    public AppSettingsWindow()
    {
        title = "Settings";

        windowFlags = EditorWindowFlags.Resizable;
    }

    private void General()
    {
        projectAppSettings.appName = EditorGUI.TextField("App name", "AppSettings.General.AppName", projectAppSettings.appName ?? "");

        projectAppSettings.companyName = EditorGUI.TextField("Company name", "AppSettings.General.CompanyName",
            projectAppSettings.companyName ?? "");

        projectAppSettings.appBundleID = EditorGUI.TextField("App bundle ID", "AppSettings.General.BundleID",
            projectAppSettings.appBundleID ?? "");

        projectAppSettings.appDisplayVersion = EditorGUI.TextField("App display version", "AppSettings.General.DisplayVersion",
            projectAppSettings.appDisplayVersion ?? "");

        projectAppSettings.appVersion = EditorGUI.IntField("App version ID", "AppSettings.General.VersionID", projectAppSettings.appVersion);

        projectAppSettings.profilingMode = EditorGUI.EnumDropdown("Profiling", "AppSettings.General.Profiling",
            projectAppSettings.profilingMode);

        projectAppSettings.overrideNativeInstructionSetX64 = EditorGUI.Toggle(
            "Override x86_64 native instruction level (default is x86_64-v3)", "AppSettings.General.Overridex64",
            projectAppSettings.overrideNativeInstructionSetX64);

        if(projectAppSettings.overrideNativeInstructionSetX64)
        {
            projectAppSettings.x64InstructionLevel = EditorGUI.EnumDropdown("Native Instruction Level (x86_64)",
                "AppSettings.General.NativeFlagsX64", projectAppSettings.x64InstructionLevel);
        }

        var lastUnsafeCode = projectAppSettings.allowUnsafeCode;

        projectAppSettings.allowUnsafeCode = EditorGUI.Toggle("Allow unsafe code", "AppSettings.General.AllowUnsafeCode",
            projectAppSettings.allowUnsafeCode);

        if (lastUnsafeCode != projectAppSettings.allowUnsafeCode)
        {
            EditorUtils.RefreshAssets(true, null);
        }
    }

    private void Timing()
    {
        projectAppSettings.fixedTimeFrameRate = EditorGUI.IntField("Fixed time frame rate", "AppSettings.Timing.FixedTimeFrameRate",
            projectAppSettings.fixedTimeFrameRate);

        if (projectAppSettings.fixedTimeFrameRate <= 0)
        {
            projectAppSettings.fixedTimeFrameRate = 1;
        }

        projectAppSettings.maximumFixedTimestepTime = EditorGUI.FloatField("Maximum time spent on fixed timesteps",
            "AppSettings.Timing.MaximumTimestep", projectAppSettings.maximumFixedTimestepTime);

        if (projectAppSettings.maximumFixedTimestepTime <= 0)
        {
            projectAppSettings.maximumFixedTimestepTime = 0.1f;
        }
    }

    private void Physics()
    {
        var previousFrameRate = projectAppSettings.physicsFrameRate;

        projectAppSettings.physicsFrameRate = EditorGUI.IntField("Physics frame rate", "AppSettings.Physics.FrameRate",
            projectAppSettings.physicsFrameRate);

        if (projectAppSettings.physicsFrameRate <= 0)
        {
            projectAppSettings.physicsFrameRate = 1;
        }

        var previousInterpolation = projectAppSettings.usePhysicsInterpolation;

        projectAppSettings.usePhysicsInterpolation = EditorGUI.Toggle("Interpolate physics", "AppSettings.Physics.Interpolate",
            projectAppSettings.usePhysicsInterpolation);

        if (previousFrameRate != projectAppSettings.physicsFrameRate ||
            previousInterpolation != projectAppSettings.usePhysicsInterpolation)
        {
            Physics3D.Instance.UpdateConfiguration();
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

            LayerMask.SetLayers(CollectionsMarshal.AsSpan(projectAppSettings.layers),
                CollectionsMarshal.AsSpan(projectAppSettings.sortingLayers));

            StapleEditor.instance.AddEditorLayers();
        }

        EditorGUI.Label("Layers");

        Handle(projectAppSettings.layers);

        EditorGUI.Label("Sorting Layers");

        Handle(projectAppSettings.sortingLayers);
    }

    private void Lighting()
    {
        {
            var current = projectAppSettings.enableLighting;

            projectAppSettings.enableLighting = EditorGUI.Toggle("Enable lighting", "AppSettings.Lighting.EnableLighting", current);

            if (projectAppSettings.enableLighting != current)
            {
                LightSystem.Enabled = projectAppSettings.enableLighting;
            }
        }

        {
            var current = projectAppSettings.ambientLight;

            projectAppSettings.ambientLight = EditorGUI.ColorField("Ambient color", "AppSettings.Lighting.AmbientColor",
                projectAppSettings.ambientLight);

            if (projectAppSettings.ambientLight != current)
            {
                AppSettings.Current.ambientLight = projectAppSettings.ambientLight;
            }
        }
    }

    private void Rendering()
    {
        projectAppSettings.runInBackground = EditorGUI.Toggle("Run in background", $"AppSettings.Rendering.Background",
            projectAppSettings.runInBackground);

        projectAppSettings.multiThreadedRenderer = EditorGUI.Toggle("Multithreaded renderer (experimental)", $"AppSettings.Rendering.Multithreaded",
            projectAppSettings.multiThreadedRenderer);

        projectAppSettings.allowFullscreenSwitch = EditorGUI.Toggle("Allow fullscreen switch", "AppSettings.Rendering.AllowFullscreenSwitch",
            projectAppSettings.allowFullscreenSwitch);

        EditorGUI.TabBar(PlayerBackendManager.BackendNames, "AppSettings.Rendering.Backends",
            (index) =>
            {
                var backend = PlayerBackendManager.Instance.GetBackend(PlayerBackendManager.BackendNames[index]);

                if (backend.platform == AppPlatform.Windows ||
                    backend.platform == AppPlatform.Linux ||
                    backend.platform == AppPlatform.MacOSX)
                {
                    projectAppSettings.defaultWindowMode = EditorGUI.EnumDropdown("Window mode *",
                        $"AppSettings.Rendering.Backend{index}.WindowMode", projectAppSettings.defaultWindowMode);

                    projectAppSettings.defaultWindowWidth = EditorGUI.IntField("Window width *",
                        $"AppSettings.Rendering.Backend{index}.WindowWidth", projectAppSettings.defaultWindowWidth);

                    projectAppSettings.defaultWindowHeight = EditorGUI.IntField("Window height *",
                        $"AppSettings.Rendering.Backend{index}.WindowHeight", projectAppSettings.defaultWindowHeight);
                }
                else if (backend.platform == AppPlatform.Android ||
                    backend.platform == AppPlatform.iOS)
                {
                    projectAppSettings.portraitOrientation = EditorGUI.Toggle("Portrait orientation *",
                        $"AppSettings.Rendering.Backend{index}.Portrait", projectAppSettings.portraitOrientation);

                    projectAppSettings.landscapeOrientation = EditorGUI.Toggle("Landscape orientation *",
                        $"AppSettings.Rendering.Backend{index}.Landscape", projectAppSettings.landscapeOrientation);

                    if (backend.platform == AppPlatform.Android)
                    {
                        projectAppSettings.androidMinSDK = EditorGUI.IntField("Minimum Android SDK",
                            $"AppSettings.Rendering.Backend{index}.AndroidSDK", projectAppSettings.androidMinSDK);

                        if (projectAppSettings.androidMinSDK < 26)
                        {
                            projectAppSettings.androidMinSDK = 26;
                        }
                    }
                    else if (backend.platform == AppPlatform.iOS)
                    {
                        projectAppSettings.iOSDeploymentTarget = EditorGUI.IntField("iOS deployment target",
                            $"AppSettings.Rendering.Backend{index}.iOSDeploymentTarget", projectAppSettings.iOSDeploymentTarget);

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
                    var result = EditorGUI.EnumDropdown("Renderer", $"AppSettings.Rendering.Renderer{index}{i}", renderers[i],
                        backend.renderers);

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
            }, null);
    }

    public override void OnGUI()
    {
        EditorGUI.Label("* - Shared setting between platforms");

        EditorGUI.TabBar(sections, "AppSettings.Sections",
            (tabIndex) =>
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

                    case string str when str == LightingKey:

                        Lighting();

                        break;

                    case string str when str == RenderingKey:

                        Rendering();

                        break;
                }
            }, null);

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
