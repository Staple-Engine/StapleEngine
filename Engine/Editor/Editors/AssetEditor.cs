using Newtonsoft.Json;
using System;
using System.IO;

namespace Staple.Editor;

public class AssetEditor : Editor
{
    internal Func<object> recreateOriginal;

    internal object RecreateOriginal()
    {
        if(recreateOriginal != null)
        {
            try
            {
                return recreateOriginal();
            }
            catch (Exception)
            {
            }
        }

        return default;
    }

    public virtual void ApplyChanges()
    {
        try
        {
            var text = JsonConvert.SerializeObject(target, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(path, text);
        }
        catch (Exception)
        {
        }
    }

    public void ShowAssetUI(Action refreshed)
    {
        var hasChanges = original.Equals(target) == false;

        if(hasChanges)
        {
            EditorGUI.Button("Apply", "AssetApply", () =>
            {
                ApplyChanges();

                EditorUtils.RefreshAssets(path.EndsWith(".asmdef"), refreshed);

                original = RecreateOriginal() ?? original;
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", "AssetRevert", () =>
            {
                target = RecreateOriginal() ?? target;

                EditorGUI.pendingObjectPickers.Clear();
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", "AssetApply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", "AssetRevert", null);
        }
    }
}
