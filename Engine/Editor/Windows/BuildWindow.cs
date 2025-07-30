using Hexa.NET.ImGui;
using Newtonsoft.Json;
using NfdSharp;
using Staple.Jobs;
using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor;

internal class BuildWindow : EditorWindow
{
    public string basePath;

    public static WeakReference<BuildWindow> instance;

    internal List<string> scenes = [];

    private bool needsLoadScenes = true;

    public BuildWindow()
    {
        title = "Build";

        instance = new WeakReference<BuildWindow>(this);

        windowFlags = EditorWindowFlags.Resizable;
    }

    public void AddScene(string guid)
    {
        if (scenes.Contains(guid))
        {
            return;
        }

        scenes.Add(guid);

        UpdateSceneList();
    }

    public void UpdateSceneList()
    {
        for (var i = scenes.Count - 1; i >= 0; i--)
        {
            if (AssetDatabase.GetAssetName(scenes[i]) == null)
            {
                scenes.RemoveAt(i);
            }
        }

        try
        {
            var json = JsonConvert.SerializeObject(scenes, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(Path.Combine(basePath, "Settings", "SceneList.json"), json);
        }
        catch (Exception)
        {

        }
    }

    public override void OnGUI()
    {
        if(needsLoadScenes)
        {
            needsLoadScenes = false;

            try
            {
                var json = File.ReadAllText(Path.Combine(basePath, "Settings", "SceneList.json"));

                scenes = JsonConvert.DeserializeObject<List<string>>(json);
            }
            catch (Exception)
            {
            }

            UpdateSceneList();
        }

        if (scenes != null)
        {
            if(ImGui.BeginListBox("Scenes"))
            {
                foreach(var guid in scenes)
                {
                    var assetName = AssetDatabase.GetAssetName(guid);

                    ImGui.Text(assetName ?? guid);

                    ImGui.SameLine();

                    if(ImGui.SmallButton("-"))
                    {
                        scenes.Remove(guid);

                        UpdateSceneList();

                        break;
                    }
                }

                ImGui.EndListBox();

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("ASSET");

                    unsafe
                    {
                        if (payload.Handle != null && StapleEditor.instance.dragDropPayloads.TryGetValue("ASSET", out var p))
                        {
                            ProjectBrowser.dropType = ProjectBrowserDropType.SceneList;

                            p.action(p.index, p.item);

                            StapleEditor.instance.dragDropPayloads.Clear();

                            StapleEditor.instance.dropTargetEntity = default;
                        }
                    }

                    ImGui.EndDragDropTarget();
                }
            }
        }

        var current = Array.IndexOf(PlayerBackendManager.BackendNames, StapleEditor.instance.buildBackend);

        current = EditorGUI.Dropdown("Platform", "BuildWindowPlatform", PlayerBackendManager.BackendNames, current);

        if (current >= 0 && current < PlayerBackendManager.BackendNames.Length)
        {
            StapleEditor.instance.buildBackend = PlayerBackendManager.BackendNames[current];
        }

        StapleEditor.instance.buildPlayerDebug = EditorGUI.Toggle("Debug build", "BuildWindowDebug", StapleEditor.instance.buildPlayerDebug);

        var has = Platform.CurrentPlatform == StapleEditor.instance.currentPlatform;
        EditorGUI.Disabled(has == false, () =>
        {
            StapleEditor.instance.buildPlayerNativeAOT = EditorGUI.Toggle("Native build", "BuildWindowNativeBuild", StapleEditor.instance.buildPlayerNativeAOT);

            if(has == false)
            {
                StapleEditor.instance.buildPlayerNativeAOT = false;
            }
        });

        StapleEditor.instance.buildPlayerDebugRedists = EditorGUI.Toggle("Use debug redistributables", "BuildWindowDebugRedist", StapleEditor.instance.buildPlayerDebugRedists);
        StapleEditor.instance.buildPlayerSingleFile = EditorGUI.Toggle("Publish single file", "BuildWindowSingleFile", StapleEditor.instance.buildPlayerSingleFile);

        var backend = PlayerBackendManager.Instance.GetBackend(StapleEditor.instance.buildBackend);

        if(backend == null)
        {
            return;
        }

        EditorGUI.Button("Build", "BuildWindowBuild", () =>
        {
            var result = Nfd.PickFolder(Path.GetFullPath(StapleEditor.instance.lastPickedBuildDirectories.TryGetValue(backend.platform, out var p) ? p : basePath),
                out var path);

            if (result == Nfd.NfdResult.NFD_OKAY)
            {
                StapleEditor.instance.lastPickedBuildDirectories.AddOrSetKey(backend.platform, path);

                StapleEditor.instance.UpdateLastSession();

                StapleEditor.instance.StartBackgroundTask(JobScheduler.Schedule(new ActionJob(() =>
                {
                    ProjectManager.Instance.BuildPlayer(backend, StapleEditor.instance.projectAppSettings, path,
                        StapleEditor.instance.buildPlayerDebug, StapleEditor.instance.buildPlayerNativeAOT,
                        StapleEditor.instance.buildPlayerDebugRedists, false, StapleEditor.instance.buildPlayerSingleFile,
                        StapleEditor.instance.SetBackgroundProgress,
                        (message) => StapleEditor.instance.ShowMessageBox(message, "OK", null),
                        (platform, finish) => StapleEditor.instance.RefreshStaging(platform, finish));
                })));
            }
            else
            {
                Log.Error($"Failed to open file dialog: {Nfd.GetError()}");
            }
        });

        EditorGUI.SameLine();

        EditorGUI.Button("Build (Assets Only)", "BuildWindowBuildAssets", () =>
        {
            var result = Nfd.PickFolder(Path.GetFullPath(StapleEditor.instance.lastPickedBuildDirectories.TryGetValue(backend.platform, out var p) ? p : basePath),
                out var path);

            if (result == Nfd.NfdResult.NFD_OKAY)
            {
                StapleEditor.instance.lastPickedBuildDirectories.AddOrSetKey(backend.platform, path);

                StapleEditor.instance.UpdateLastSession();

                StapleEditor.instance.StartBackgroundTask(JobScheduler.Schedule(new ActionJob(() =>
                {
                    ProjectManager.Instance.BuildPlayer(backend, StapleEditor.instance.projectAppSettings, path,
                        StapleEditor.instance.buildPlayerDebug, StapleEditor.instance.buildPlayerNativeAOT,
                        StapleEditor.instance.buildPlayerDebugRedists, true, StapleEditor.instance.buildPlayerSingleFile,
                        StapleEditor.instance.SetBackgroundProgress,
                        (message) => StapleEditor.instance.ShowMessageBox(message, "OK", null),
                        (platform, finish) => StapleEditor.instance.RefreshStaging(platform, finish));
                })));
            }
            else
            {
                Log.Error($"Failed to open file dialog: {Nfd.GetError()}");
            }
        });
    }
}
