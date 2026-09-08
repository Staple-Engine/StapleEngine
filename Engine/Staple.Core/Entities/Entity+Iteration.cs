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
    /// Iterates through the <see cref="CallbackComponent"/> components of this entity
    /// </summary>
    /// <param name="callback">A callback to handle the component</param>
    public readonly void IterateCallableComponents(World.SingleCallableComponentCallback callback)
    {
        World.Current?.IterateCallableComponents(this, callback);
    }
}
