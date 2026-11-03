using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[AbstractComponent]
public class Component
{
    public bool Enabled
    {
        get;

        set
        {
            field = value;

            World.Current?.ReportChangedComponent(this);
        }
    }

    [NonSerialized]
    public Entity Entity { get; internal set; }

    [NonSerialized]
    public Transform Transform { get; internal set; }
}
