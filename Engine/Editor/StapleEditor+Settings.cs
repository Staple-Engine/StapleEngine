using Newtonsoft.Json;
using System.IO;
using System;

namespace Staple.Editor;

internal partial class StapleEditor
{
    public void LoadEditorSettings()
    {
        try
        {
            editorSettings = JsonConvert.DeserializeObject<EditorSettings>(File.ReadAllText(Path.Combine(Storage.PersistentDataPath, "EditorSettings.json")),
                Tooling.Utilities.JsonSettings);
        }
        catch (Exception)
        {
            editorSettings = new();

            var path = Environment.GetEnvironmentVariable("PATH");

            if(path != null)
            {
                string[] pieces = [];
                var extension = "";

                if(Platform.IsWindows)
                {
                    pieces = path.Split(';');
                    extension = ".exe";
                }
                else
                {
                    pieces = path.Split(':');
                }

                foreach(var piece in pieces)
                {
                    var t = Path.Combine(piece, $"git{extension}");

                    if (File.Exists(t))
                    {
                        editorSettings.gitExternalPath = t;
                    }
                }
            }

            SaveEditorSettings();
        }
    }

    public void SaveEditorSettings()
    {
        try
        {
            File.WriteAllText(Path.Combine(Storage.PersistentDataPath, "EditorSettings.json"), JsonConvert.SerializeObject(editorSettings, Formatting.Indented,
                Tooling.Utilities.JsonSettings));
        }
        catch(Exception)
        {
        }
    }
}
