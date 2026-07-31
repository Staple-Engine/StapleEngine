using Newtonsoft.Json;
using System;
using System.IO;

namespace Staple.Editor;

public class AssetEditor : Editor
{
    public bool isMetaEditor = false;

    public bool HasChanges => !original.Equals(target);

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

            var path = isMetaEditor ? $"{this.path}.meta" : this.path;

            File.WriteAllText(path, text);
        }
        catch (Exception)
        {
        }
    }

    public void ShowAssetUI(Action refreshed, Action reverted = null)
    {
        if(HasChanges)
        {
            EditorGUI.Button("Apply", "AssetApply", () =>
            {
                ApplyChanges();

                EditorUtils.RefreshAssets(path.EndsWith(".asmdef") || path.EndsWith(".dll"), refreshed);

                original = RecreateOriginal() ?? original;
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", "AssetRevert", () =>
            {
                target = RecreateOriginal() ?? target;

                EditorGUI.pendingObjectPickers.Clear();

                try
                {
                    reverted?.Invoke();
                }
                catch(Exception e)
                {
                    Log.Error($"Revert callback exception for {GetType().FullName}: {e}", "Asset Editor");
                }
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
