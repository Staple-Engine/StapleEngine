using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Component
{
    /// <summary>
    /// Flag for knowing when to emit the Start() event
    /// There are better ways to do this, but for now this works!
    /// </summary>
    internal bool STAPLE_JUST_ADDED = true;

    public bool enabled = true;

    [field: NonSerialized]
    public Entity Entity { get; internal set; }

    [field: NonSerialized]
    public Transform Transform { get; internal set; }
}
