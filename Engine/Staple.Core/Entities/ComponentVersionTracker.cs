using System;

namespace Staple;

public class ComponentVersionTracker<T> where T: IComponent, IComponentVersion
{
    private ulong[] versions = [1024];

    public bool ShouldUpdateComponent(Entity entity, in T component)
    {
        if (component == null)
        {
            throw new ArgumentNullException(nameof(component), "Component can't be null");
        }

        if(versions.Length < entity.Identifier.ID)
        {
            Array.Resize(ref versions, entity.Identifier.ID);
        }

        var version = versions[entity.Identifier.ID - 1];

        versions[entity.Identifier.ID - 1] = component.Version;

        return version != component.Version;
    }
}
