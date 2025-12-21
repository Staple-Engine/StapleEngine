using System;

namespace Staple;

public class ComponentVersionTracker<T> where T: IComponent, IComponentVersion
{
    private ulong[] versions = new ulong[1024];

    public bool ShouldUpdateComponent(Entity entity, in T component)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component), "Component can't be null");
        }

        if(versions.Length < entity.Identifier.ID)
        {
            Array.Resize(ref versions, versions.Length * 2);
        }

        var componentVersion = component.Version;
        var index = entity.Identifier.ID - 1;

        var version = versions[index];

        if(version != componentVersion)
        {
            versions[index] = componentVersion;

            return true;
        }

        return false;
    }
}
