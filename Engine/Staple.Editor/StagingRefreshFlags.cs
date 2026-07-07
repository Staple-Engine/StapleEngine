using System;

namespace Staple.Editor;

[Flags]
internal enum StagingRefreshFlags
{
    None = 0,
    UpdateProject = (1 << 0),
    CheckBuild = (1 << 1),
    LoadLastScene = (1 << 2),
    ReloadScene = (1 << 3),
}
