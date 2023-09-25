using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(Sprite))]
    internal class SpriteEditor : Editor
    {
        public override bool RenderField(FieldInfo field)
        {
            switch(field.Name)
            {
                case nameof(Sprite.sortingLayer):

                    {
                        var value = (uint)field.GetValue(target);

                        value = (uint)EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), LayerMask.AllSortingLayers.ToArray(), (int)value);

                        field.SetValue(target, value);

                        return true;
                    }

                case nameof(Sprite.texture):

                    {
                        var value = (Texture)field.GetValue(target);

                        value = (Texture)EditorGUI.ObjectPicker(field.FieldType, field.Name.ExpandCamelCaseName(), value);

                        field.SetValue(target, value);
                    }

                    return true;
            }

            return false;
        }
    }
}
