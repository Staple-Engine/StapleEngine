using System;

namespace Staple.Editor;

/// <summary>
/// Describes a menu item for some editor components such as editor windows.
/// Describe the location of the menu by separating sections with `/`.
//  Example:
//    My Menu/My Item
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class MenuItemAttribute : Attribute
{
    public string path;

    public MenuItemAttribute(string path)
    {
        this.path = path;
    }
}
