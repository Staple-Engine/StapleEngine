using System;
using System.Collections.Generic;

namespace Staple;

public class ComponentVersionTracker
{
    private readonly Dictionary<int, ulong> versions = [];

    public void Clear()
    {
        versions.Clear();
    }

    public bool ShouldUpdateComponent<T>(Entity entity, in T component) where T : IComponent, IComponentVersion
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component), "Component can't be null");
        }

        var hashCode = HashCode.Combine(entity, component.GetType().FullName.GetHashCode());

        if (versions.TryGetValue(hashCode, out var version) == false)
        {
            versions.Add(hashCode, component.Version);

            return true;
        }

        return version != component.Version;
    }
}
