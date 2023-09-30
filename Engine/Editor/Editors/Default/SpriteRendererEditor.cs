using System;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(SpriteRenderer))]
    internal class SpriteRendererEditor : Editor
    {
        public override bool RenderField(FieldInfo field)
        {
            var renderer = target as SpriteRenderer;

            switch(field.Name)
            {
                case nameof(SpriteRenderer.sortingLayer):

                    {
                        var value = (uint)field.GetValue(target);

                        value = (uint)EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), LayerMask.AllSortingLayers.ToArray(), (int)value);

                        field.SetValue(target, value);

                        return true;
                    }

                case nameof(SpriteRenderer.texture):

                    {
                        var value = (Texture)field.GetValue(target);

                        value = (Texture)EditorGUI.ObjectPicker(field.FieldType, field.Name.ExpandCamelCaseName(), value);

                        field.SetValue(target, value);

                        if (value != null)
                        {
                            EditorGUI.Label("Selected Sprite");

                            EditorGUI.SameLine();

                            if (renderer.spriteIndex >= 0 && renderer.spriteIndex < value.metadata.sprites.Count)
                            {
                                EditorGUI.TextureRect(value, value.metadata.sprites[renderer.spriteIndex].rect, new Vector2(32, 32));
                            }
                            else
                            {
                                EditorGUI.Label("(none)");
                            }

                            EditorGUI.SameLine();

                            if (EditorGUI.Button("O##SpritePicker"))
                            {
                                if(StapleEditor.instance.TryGetTarget(out var editor))
                                {
                                    var assetPath = StapleEditor.GetAssetPathFromCache(value.path);

                                    editor.showingSpritePicker = true;
                                    editor.spritePickerTexture = ThumbnailCache.GetTexture(assetPath) ?? value;
                                    editor.spritePickerSprites = value.metadata.sprites;
                                    editor.spritePickerCallback = (index) => renderer.spriteIndex = index;
                                }
                            }
                        }
                    }

                    return true;

                case nameof(SpriteRenderer.spriteIndex):

                    return true;
            }

            return false;
        }
    }
}
