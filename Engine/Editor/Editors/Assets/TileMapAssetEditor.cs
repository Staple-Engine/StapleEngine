using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(TileMapAsset))]
    internal class TileMapAssetEditor : StapleAssetEditor
    {
        public override bool RenderField(FieldInfo field)
        {
            if(field.Name == nameof(TileMapAsset.tilesets))
            {
                var tilesets = (List<Texture>)field.GetValue(target);

                if (EditorGUI.Button("+"))
                {
                    tilesets.Add(null);
                }

                ImGui.BeginGroup();

                for (var i = 0; i < tilesets.Count; i++)
                {
                    if (EditorGUI.Button("-"))
                    {
                        tilesets.RemoveAt(i);

                        break;
                    }

                    EditorGUI.SameLine();

                    tilesets[i] = (Texture)EditorGUI.ObjectPicker(typeof(Texture), $"Texture {i}", tilesets[i]);
                }

                ImGui.EndGroup();

                return true;
            }

            return base.RenderField(field);
        }
    }
}
