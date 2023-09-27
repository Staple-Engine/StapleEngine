using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(SpriteRenderer))]
    internal class SpriteRendererEditor : Editor
    {
        public override bool RenderField(FieldInfo field)
        {
            switch(field.Name)
            {
                case nameof(SpriteRenderer.sortingLayer):

                    {
                        var value = (uint)field.GetValue(target);

                        value = (uint)EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), LayerMask.AllSortingLayers.ToArray(), (int)value);

                        field.SetValue(target, value);

                        return true;
                    }

                case nameof(SpriteRenderer.sprite):

                    {
                        var value = (Sprite)field.GetValue(target);

                        value = (Sprite)EditorGUI.ObjectPicker(field.FieldType, field.Name.ExpandCamelCaseName(), value);

                        field.SetValue(target, value);
                    }

                    return true;
            }

            return false;
        }
    }
}
