using System;

namespace Staple;

/// <summary>
/// Adds a tooltip to a field to display in the inspector
/// </summary>
/// <param name="caption">The text to display</param>
[AttributeUsage(AttributeTargets.Field)]
public class TooltipAttribute(string caption) : Attribute
{
    public string caption = caption;
}
