using System;

namespace Staple.Editor;

/// <summary>
/// Defines which type an editor is for.
/// Example:
/// [CustomEditor(typeof(SpriteAsset))]
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CustomEditorAttribute : Attribute
{
    public Type target;

    public CustomEditorAttribute(Type target)
    {
        this.target = target;
    }
}
