using Staple.PackageManagement;

namespace Staple.Editor;

internal class EditorSettingsWindow : EditorWindow
{
    public StapleEditor.EditorSettings editorSettings;
    public string gitExternalPath;
    public bool autoRecompile;

    private bool firstFrame = true;

    public override void OnGUI()
    {
        if(firstFrame)
        {
            firstFrame = false;

            gitExternalPath = editorSettings.gitExternalPath;
            autoRecompile = editorSettings.autoRecompile;
        }

        gitExternalPath = EditorGUI.TextField("Git external path", "EditorSettingsWindow.GitPath", gitExternalPath);

        EditorGUI.SameLine();

        EditorGUI.Button("Browse", "EditorSettingsWindow.GitPath", () =>
        {
            var extension = Platform.IsWindows ? ".exe" : "";

            Platform.platformProvider.ShowOpenFileDialog("Browse for Git executable", null, null, gitExternalPath,
                new()
                {
                    { Platform.IsWindows ? "Executables" : "All", Platform.IsWindows ? "exe" : "*" },
                },
                false,
                (results) =>
                {
                    var path = results[0];

                    if (path.EndsWith($"git{extension}"))
                    {
                        gitExternalPath = path;
                    }
                },
                null);
        });

        autoRecompile = EditorGUI.Toggle("Auto Recompile", "EditorSettingsWindow.AutoRecompile", autoRecompile);

        EditorGUI.Tooltip("Whether the editor should recompile the game when code files are changed or moved");

        EditorGUI.Button("Apply Changes", "EditorSettings.ApplyChanges", () =>
        {
            editorSettings.gitExternalPath = gitExternalPath;
            editorSettings.autoRecompile = autoRecompile;

            PackageManager.instance.gitPath = editorSettings.gitExternalPath;

            StapleEditor.instance.SaveEditorSettings();
        });
    }
}
