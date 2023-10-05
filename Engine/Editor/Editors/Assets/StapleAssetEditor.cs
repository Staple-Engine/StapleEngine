using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(IStapleAsset))]
    internal class StapleAssetEditor : Editor
    {
        public bool ApplyChanges() => StapleEditor.SaveAsset(path, (IStapleAsset)target);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var asset = (IStapleAsset)target;
            var originalAsset = (IStapleAsset)original;

            var hasChanges = asset != originalAsset;

            if (hasChanges)
            {
                if (EditorGUI.Button("Apply"))
                {
                    if(ApplyChanges())
                    {
                        var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                        foreach (var field in fields)
                        {
                            field.SetValue(originalAsset, field.GetValue(asset));
                        }

                        EditorUtils.RefreshAssets(false, null);
                    }
                }

                EditorGUI.SameLine();

                if (EditorGUI.Button("Revert"))
                {
                    var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        field.SetValue(asset, field.GetValue(originalAsset));
                    }
                }
            }
            else
            {
                EditorGUI.ButtonDisabled("Apply");

                EditorGUI.SameLine();

                EditorGUI.ButtonDisabled("Revert");
            }
        }
    }
}
