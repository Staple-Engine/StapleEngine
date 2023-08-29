using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(Sprite))]
    internal class SpriteEditor : Editor
    {
        public override bool RenderField(FieldInfo field)
        {
            if(field.Name == "sortingLayer")
            {
                var value = (uint)field.GetValue(target);

                value = (uint)EditorGUI.Dropdown(field.Name, LayerMask.AllSortingLayers.ToArray(), (int)value);

                field.SetValue(target, value);

                return true;
            }

            return false;
        }
    }
}
