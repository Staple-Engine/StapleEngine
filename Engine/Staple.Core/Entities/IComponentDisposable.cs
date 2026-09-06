namespace Staple;

/// <summary>
/// Interface for components that should be cleaned up.
/// You must combine this with <see cref="Component"/>.
/// </summary>
public interface IComponentDisposable
{
    void DisposeComponent();
}
