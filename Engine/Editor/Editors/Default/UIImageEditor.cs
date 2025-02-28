using Staple.Internal;
using Staple.UI;
using System;

namespace Staple.Editor;

[CustomEditor(typeof(UIImage))]
internal class UIImageEditor : Editor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var image = target as UIImage;

        switch (name)
        {
            case nameof(UIImage.sprite):

                setter(EditorGUI.SpritePicker(name, getter() as Sprite, $"{name}.SpritePicker"));

                return true;

            case nameof(UIImage.material):

                image.material ??= SpriteRenderSystem.DefaultMaterial.Value;

                return false;
        }

        return false;
    }
}
