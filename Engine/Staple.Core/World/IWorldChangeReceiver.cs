namespace Staple;

/// <summary>
/// Receive events related to the world changing
/// </summary>
public interface IWorldChangeReceiver
{
    /// <summary>
    /// The world was completely replaced, and should be treated as new
    /// </summary>
    /// <param name="world">The new world. It can be null if the world failed to load.</param>
    /// <remarks>The <see cref="WorldChanged(World)"/> event will likely be called after this</remarks>
    void WorldReplaced(World world);

    /// <summary>
    /// An entity was created/destroyed/activated/deactivated/layer changed, a component was replaced/added/removed
    /// </summary>
    /// <param name="world">The world we're handling right now. It can be null if the world failed to load.</param>
    void WorldChanged(World world);
}
