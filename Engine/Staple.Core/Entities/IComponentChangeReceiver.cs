namespace Staple;

public interface IComponentChangeReceiver
{
    /// <summary>
    /// The hash of the component type
    /// </summary>
    int ComponentHash { get; }

    /// <summary>
    /// Mark a component as changed
    /// </summary>
    /// <param name="component">The component that changed</param>
    void MarkComponentChanged(VersionableComponent component);
}
