using System;

namespace Staple.UI;

/// <summary>
/// How to align UI Text
/// </summary>
[Flags]
public enum UITextAlignment
{
    /// <summary>
    /// Left
    /// </summary>
    Left = (1 << 0),
    /// <summary>
    /// Right
    /// </summary>
    Right = (1 << 1),
    /// <summary>
    /// Center
    /// </summary>
    Center = (1 << 2),
    /// <summary>
    /// Vertical center
    /// </summary>
    VCenter = (1 << 3),
}
