using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public partial class Component
{
    public partial bool Enabled { get; set; }

    [field: NonSerialized]
    public Entity Entity { get; internal set; }

    [field: NonSerialized]
    public Transform Transform { get; internal set; }
}
