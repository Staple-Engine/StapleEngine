using System;

namespace Staple.Editor;

[CustomEditor(typeof(SpriteRenderer))]
public class SpriteRendererEditor : Editor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var renderer = target as SpriteRenderer;

        switch(name)
        {
            case nameof(SpriteRenderer.sprite):

                setter(EditorGUI.SpritePicker(name, getter() as Sprite, $"{name}.SpritePicker"));

                return true;

            case nameof(SpriteRenderer.material):

                renderer.material ??= SpriteUtils.DefaultMaterial.Value;

                return false;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var renderer = (SpriteRenderer)target;

        EditorGUI.Label($"Bounds: Center: {renderer.bounds.center} Size: {renderer.bounds.size}");
    }
}
