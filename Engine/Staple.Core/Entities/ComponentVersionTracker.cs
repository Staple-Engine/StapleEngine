using System;
using System.Runtime.CompilerServices;

namespace Staple;

public class ComponentVersionTracker<T> where T: IComponent, IComponentVersion
{
    private readonly ExpandableContainer<ulong> versions = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldUpdateComponent(Entity entity, in T component)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component), "Component can't be null");
        }

        var index = entity.Identifier.ID - 1;
        var componentVersion = component.Version;

        if (index >= versions.Length)
        {
            versions.Resize(index + 1, true);
        }

        var contents = versions.Contents;

        ref var version = ref contents[index];

        if(version != componentVersion)
        {
            version = componentVersion;

            return true;
        }

        return false;
    }

    public void Clear()
    {
        versions.ClearValues();
    }
}
