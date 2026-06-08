namespace Staple;

/// <summary>
/// Contains rendering information for a render system
/// </summary>
/// <param name="entity">The entity we're rendering with</param>
/// <param name="transform">The transform of the entity</param>
/// <param name="component">The component</param>
public readonly struct RenderEntry(Entity entity, Transform transform, IComponent component)
{
    public readonly Entity entity = entity;
    public readonly Transform transform = transform;
    public readonly IComponent component = component;
}
