using System;

namespace Staple;

/// <summary>
/// Renders a button in the editor when attached to a function without parameters
/// </summary>
/// <param name="title">The title for the button</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class ButtonAttribute(string title) : Attribute
{
    public readonly string title = title;
}
