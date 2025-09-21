using System;

namespace Staple;

/// <summary>
/// Adds header text before a field to display in the inspector
/// </summary>
/// <param name="caption">The text to display</param>
[AttributeUsage(AttributeTargets.Field)]
public class HeaderAttribute(string caption) : Attribute
{
    public string caption = caption;
}
