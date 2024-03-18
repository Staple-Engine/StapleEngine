using System.Collections.Generic;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(TileMapAsset))]
internal class TileMapAssetEditor : StapleAssetEditor
{
    public override bool RenderField(FieldInfo field)
    {
        if(field.Name == nameof(TileMapAsset.tilesets))
        {
            var tilesets = (List<Texture>)field.GetValue(target);

            EditorGUI.Button("+", () =>
            {
                tilesets.Add(null);
            });

            EditorGUI.Group(() =>
            {
                for (var i = 0; i < tilesets.Count; i++)
                {
                    EditorGUI.Button("-", () =>
                    {
                        tilesets.RemoveAt(i);
                    });

                    EditorGUI.SameLine();

                    tilesets[i] = (Texture)EditorGUI.ObjectPicker(typeof(Texture), $"Texture {i}", tilesets[i]);
                }
            });

            return true;
        }

        return base.RenderField(field);
    }
}
