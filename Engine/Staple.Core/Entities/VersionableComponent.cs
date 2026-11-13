namespace Staple;

public class VersionableComponent : Component
{
    [NonSerialized]
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
