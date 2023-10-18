using Staple.Internal;
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
                                var sprite = value.metadata.sprites[renderer.spriteIndex];

                                EditorGUI.TextureRect(value, sprite.rect, new Vector2(32, 32), sprite.rotation);
                            }
                            else
                            {
                                EditorGUI.Label("(none)");
                            }

                            EditorGUI.SameLine();

                            if (EditorGUI.Button("O##SpritePicker"))
                            {
                                var editor = StapleEditor.instance;
                                var assetPath = AssetSerialization.GetAssetPathFromCache(value.path);

                                if(assetPath != value.path && Path.IsPathRooted(assetPath) == false)
                                {
                                    assetPath = $"Assets{Path.DirectorySeparatorChar}{assetPath}";
                                }

                                editor.ShowSpritePicker(ThumbnailCache.GetTexture(assetPath) ?? value,
                                    value.metadata.sprites,
                                    (index) => renderer.spriteIndex = index);
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
