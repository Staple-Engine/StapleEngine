using System;

namespace Staple;

public class VersionableComponent : Component
{
    [field: NonSerialized]
    public ulong Version
    {
        get;

        set
        {
            field = value;

            World.Current?.ReportChangedComponent(this);
        }
    }
}
