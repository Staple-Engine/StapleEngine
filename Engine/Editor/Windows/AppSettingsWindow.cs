using ImGuiNET;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Staple.Internal;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    public AppSettingsWindow()
    {
        allowDocking = false;
    }

    public override void OnGUI()
    {
        base.OnGUI();

        if (ImGui.TreeNodeEx("General", ImGuiTreeNodeFlags.SpanFullWidth))
        {
            projectAppSettings.appName = EditorGUI.TextField("App Name", projectAppSettings.appName ?? "");

            projectAppSettings.companyName = EditorGUI.TextField("Company Name", projectAppSettings.companyName ?? "");

            projectAppSettings.appBundleID = EditorGUI.TextField("App Bundle ID", projectAppSettings.appBundleID ?? "");

            projectAppSettings.appDisplayVersion = EditorGUI.TextField("App Display Version", projectAppSettings.appDisplayVersion ?? "");

            projectAppSettings.appVersion = EditorGUI.IntField("App Version ID", projectAppSettings.appVersion);

            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Timing", ImGuiTreeNodeFlags.SpanFullWidth))
        {
            projectAppSettings.fixedTimeFrameRate = EditorGUI.IntField("Fixed Time Frame Rate", projectAppSettings.fixedTimeFrameRate);

            if (projectAppSettings.fixedTimeFrameRate <= 0)
            {
                projectAppSettings.fixedTimeFrameRate = 1;
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Layers", ImGuiTreeNodeFlags.SpanFullWidth))
        {
            void Handle(List<string> layers)
            {
                for (var i = 0; i < layers.Count; i++)
                {
                    layers[i] = EditorGUI.TextField($"Layer {i + 1}##{layers.GetHashCode()}{i}", layers[i]);

                    //Can't remove default layer
                    if (i > 1)
                    {
                        EditorGUI.SameLine();

                        if (EditorGUI.Button("Up##{layers.GetHashCode()}{i}"))
                        {
                            (layers[i], layers[i - 1]) = (layers[i - 1], layers[i]);
                        }
                    }

                    if (i > 0 && i + 1 < layers.Count)
                    {
                        EditorGUI.SameLine();

                        if (EditorGUI.Button("Down##{layers.GetHashCode()}{i}"))
                        {
                            (layers[i], layers[i + 1]) = (layers[i + 1], layers[i]);
                        }
                    }

                    //Can't remove default layer
                    if (i > 0)
                    {
                        EditorGUI.SameLine();

                        if (EditorGUI.Button($"X##{layers.GetHashCode()}{i}"))
                        {
                            layers.RemoveAt(i);

                            break;
                        }
                    }
                }

                if (EditorGUI.Button($"+##{layers.GetHashCode()}"))
                {
                    layers.Add("Layer");
                }

                LayerMask.AllLayers = projectAppSettings.layers;
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;
            }

            EditorGUI.Label("Layers");

            Handle(projectAppSettings.layers);

            EditorGUI.Label("Sorting Layers");

            Handle(projectAppSettings.sortingLayers);

            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Rendering and Presentation", ImGuiTreeNodeFlags.SpanFullWidth))
        {
            projectAppSettings.runInBackground = EditorGUI.Toggle("Run in Background", projectAppSettings.runInBackground);

            projectAppSettings.multiThreadedRenderer = EditorGUI.Toggle("Multithreaded Renderer (experimental)", projectAppSettings.multiThreadedRenderer);

            if (ImGui.BeginTabBar("Platforms"))
            {
                foreach (var backendName in PlayerBackendManager.BackendNames)
                {
                    var backend = PlayerBackendManager.Instance.GetBackend(backendName);

                    if (ImGui.BeginTabItem($"{backend.name}##0"))
                    {
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

                        ImGui.Text("Renderers");

                        if (projectAppSettings.renderers.TryGetValue(backend.platform, out var renderers) == false)
                        {
                            renderers = new();

                            projectAppSettings.renderers.Add(backend.platform, renderers);
                        }

                        for (var i = 0; i < renderers.Count; i++)
                        {
                            var result = EditorGUI.EnumDropdown($"Renderer##{i}", renderers[i], backend.renderers);

                            if (result != renderers[i] && renderers.All(x => x != result))
                            {
                                renderers[i] = result;
                            }

                            EditorGUI.SameLine();

                            if (EditorGUI.Button($"-##{i}"))
                            {
                                renderers.RemoveAt(i);

                                break;
                            }
                        }

                        if (EditorGUI.Button("+##Renderers"))
                        {
                            renderers.Add(backend.renderers.FirstOrDefault());
                        }

                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }

            ImGui.TreePop();
        }

        ImGui.Text("* - Shared setting between platforms");

        if (EditorGUI.Button("Apply Changes"))
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
        }

        EditorGUI.SameLine();

        if (EditorGUI.Button("Close"))
        {
            Close();
        }
    }
}
