using System;
using System.Runtime.CompilerServices;

namespace Staple;

public class ComponentVersionTracker<T> where T: IComponent, IComponentVersion
{
    private ulong[] versions = new ulong[1024];

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
            var newSize = versions.Length * 2;

            while(newSize < entity.Identifier.ID)
            {
                newSize *= 2;
            }

            Array.Resize(ref versions, newSize);
        }

        var version = versions[index];

        if(version != componentVersion)
        {
            versions[index] = componentVersion;

            return true;
        }

        return false;
    }
}
