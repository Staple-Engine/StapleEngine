using System;

namespace Staple.Editor;

/// <summary>
/// Defines which type an editor is for.
/// Example:
/// [CustomEditor(typeof(SpriteAsset))]
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CustomEditorAttribute(Type target) : Attribute
{
    public Type target = target;
}
