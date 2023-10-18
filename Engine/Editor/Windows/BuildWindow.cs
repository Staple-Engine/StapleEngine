using ImGuiNET;
using NfdSharp;
using Staple.Internal;
using System;
using System.IO;

namespace Staple.Editor
{
    internal class BuildWindow : EditorWindow
    {
        public string basePath;

        public BuildWindow()
        {
            allowDocking = false;
        }

        public override void OnGUI()
        {
            base.OnGUI();

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
}
