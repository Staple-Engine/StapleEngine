using NfdSharp;

namespace Staple.Editor;

internal class EditorSettingsWindow : EditorWindow
{
    public StapleEditor.EditorSettings editorSettings;
    public string gitExternalPath;

    private bool firstFrame = true;

    public override void OnGUI()
    {
        if(firstFrame)
        {
            firstFrame = false;

            gitExternalPath = editorSettings.gitExternalPath;
        }

        gitExternalPath = EditorGUI.TextField("Git external path", "EditorSettingsWindow.GitPath", gitExternalPath);

        EditorGUI.SameLine();

        EditorGUI.Button("Browse", "EditorSettingsWindow.GitPath", () =>
        {
            var extension = Platform.IsWindows ? ".exe" : "";

            if (Nfd.OpenDialog(Platform.IsWindows ? "exe" : "", gitExternalPath, out var path) == Nfd.NfdResult.NFD_OKAY)
            {
                if(path.EndsWith($"git{extension}"))
                {
                    gitExternalPath = path;
                }
            }
        });

        EditorGUI.Button("Apply Changes", "EditorSettings.ApplyChanges", () =>
        {
            editorSettings.gitExternalPath = gitExternalPath;

            StapleEditor.instance.SaveEditorSettings();
        });
    }
}
