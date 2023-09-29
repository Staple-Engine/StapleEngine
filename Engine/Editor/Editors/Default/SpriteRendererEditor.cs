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

                        if(value != null && renderer.spriteIndex >= 0 && renderer.spriteIndex < value.metadata.sprites.Count)
                        {
                            EditorGUI.Label("Selected Sprite");

                            EditorGUI.SameLine();

                            EditorGUI.TextureRect(value, value.metadata.sprites[renderer.spriteIndex].rect, new Vector2(32, 32));
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
