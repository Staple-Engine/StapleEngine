namespace Staple;

/// <summary>
/// Interface for components that should be cleaned up.
/// You must combine this with <see cref="IComponent"/>.
/// </summary>
public interface IComponentDisposable
{
    void DisposeComponent();
}
