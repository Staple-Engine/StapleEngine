using System;

namespace Staple.Editor;

/// <summary>
/// Defines a drawer for a property.
/// You should derive from this and apply CustomPropertyDrawer for the type or attribute type this applies to.
/// </summary>
public abstract class PropertyDrawer
{
    /// <summary>
    /// Renders a property
    /// </summary>
    /// <param name="name">The name of the property</param>
    /// <param name="ID">The current internal ID of the editor</param>
    /// <param name="getter">Gets the property value</param>
    /// <param name="setter">Call this to set the property value</param>
    /// <param name="getAttribute">Tries to get an attribute of a specific type</param>
    public abstract void OnGUI(string name, string ID, Func<object> getter, Action<object> setter, Func<Type, object> getAttribute);
}
