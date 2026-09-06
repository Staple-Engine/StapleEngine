using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Component
{
    public bool enabled = true;

    [field: NonSerialized]
    public Entity Entity { get; internal set; }

    [field: NonSerialized]
    public Transform Transform { get; internal set; }
}
