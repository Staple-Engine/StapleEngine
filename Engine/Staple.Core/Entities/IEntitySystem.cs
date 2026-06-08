using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Interface for entity systems that require a lifecycle (startup/shutdown)
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystemLifecycle
{
    void Startup();

    void Shutdown();
}

/// <summary>
/// Update-based Entity system.
/// You can implement this interface in order to modify entities on a per-frame basis.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystemUpdate
{
    void Update(float deltaTime);
}

/// <summary>
/// Fixed Update-based Entity system.
/// You can implement this interface in order to modify entities in a fixed time step.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystemFixedUpdate
{
    void FixedUpdate(float deltaTime);
}
