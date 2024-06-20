using System;

namespace Staple.Editor;

/// <summary>
/// Defines a drawer for a property.
/// You should derive from this and apply CustomPropertyDrawer for the type or attribute type this applies to.
/// </summary>
public abstract class PropertyDrawer
{
    public abstract void OnGUI(string name, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute);
}
