using ImGuiNET;
using Newtonsoft.Json;
using NfdSharp;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor;

internal class BuildWindow : EditorWindow
{
    public string basePath;

    public static WeakReference<BuildWindow> instance;

    internal List<string> scenes = new();

    private bool needsLoadScenes = true;

    public BuildWindow()
    {
        instance = new WeakReference<BuildWindow>(this);

        allowDocking = false;
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
            var json = JsonConvert.SerializeObject(scenes);

            File.WriteAllText(Path.Combine(basePath, "Settings", "SceneList.json"), json);
        }
        catch (Exception)
        {

        }
    }

    public override void OnGUI()
    {
        base.OnGUI();

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

                        break;
                    }
                }

                ImGui.EndListBox();

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("ASSET");

                    unsafe
                    {
                        if (payload.NativePtr != null && StapleEditor.instance.dragDropPayloads.TryGetValue("ASSET", out var p))
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

        current = EditorGUI.Dropdown("Platform", PlayerBackendManager.BackendNames, current);

        if (current >= 0 && current < PlayerBackendManager.BackendNames.Length)
        {
            StapleEditor.instance.buildBackend = PlayerBackendManager.BackendNames[current];
        }

        StapleEditor.instance.buildPlayerDebug = EditorGUI.Toggle("Debug Build", StapleEditor.instance.buildPlayerDebug);
        StapleEditor.instance.buildPlayerNativeAOT = EditorGUI.Toggle("Native Build", StapleEditor.instance.buildPlayerNativeAOT);

        var backend = PlayerBackendManager.Instance.GetBackend(StapleEditor.instance.buildBackend);

        if(backend == null)
        {
            return;
        }

        if (EditorGUI.Button("Build"))
        {
            var result = Nfd.PickFolder(Path.GetFullPath(StapleEditor.instance.lastPickedBuildDirectories.TryGetValue(backend.platform, out var p) ? p : basePath),
                out var path);

            if (result == Nfd.NfdResult.NFD_OKAY)
            {
                StapleEditor.instance.lastPickedBuildDirectories.AddOrSetKey(backend.platform, path);

                StapleEditor.instance.UpdateLastSession();

                StapleEditor.instance.showingProgress = true;
                StapleEditor.instance.progressFraction = 0;

                ImGui.OpenPopup("ShowingProgress");

                StapleEditor.instance.StartBackgroundTask((ref float progressFraction) =>
                {
                    StapleEditor.instance.BuildPlayer(backend, path, StapleEditor.instance.buildPlayerDebug, StapleEditor.instance.buildPlayerNativeAOT);

                    return true;
                });
            }
            else
            {
                Log.Error($"Failed to open file dialog: {Nfd.GetError()}");
            }
        }

        EditorGUI.SameLine();

        if (EditorGUI.Button("Close"))
        {
            Close();
        }
    }
}
