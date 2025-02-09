using System;

namespace Staple.Editor;

[Flags]
public enum EditorWindowFlags
{
    None = 0,
    Dockable = (1 << 0),
    Resizable = (1 << 1),
    Centered = (1 << 2),
    HasMenuBar = (1 << 3),
}
