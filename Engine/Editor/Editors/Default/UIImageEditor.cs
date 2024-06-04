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
            case nameof(UIImage.texture):

                {
                    var value = (Texture)getter();

                    EditorUtils.SpritePicker(name, ref value, ref image.spriteIndex, setter);
                }

                return true;

            case nameof(UIImage.spriteIndex):

                return true;

            case nameof(UIImage.material):

                if (image.material == null)
                {
                    image.material = ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");
                }

                return false;
        }

        return false;
    }
}
