namespace Staple;

public partial struct Entity
{
    /// <summary>
    /// Iterates through the components of this entity
    /// </summary>
    /// <param name="callback">A callback to handle the component</param>
    public readonly void IterateComponents(World.IterateComponentCallback callback)
    {
        World.Current?.IterateComponents(this, callback);
    }

    /// <summary>
    /// Iterates through the <see cref="LifecycleComponent"/> components of this entity
    /// </summary>
    /// <param name="callback">A callback to handle the component</param>
    public readonly void IterateLifecycleComponents(World.SingleLifecycleComponentCallback callback)
    {
        World.Current?.IterateLifecycleComponents(this, callback);
    }
}
