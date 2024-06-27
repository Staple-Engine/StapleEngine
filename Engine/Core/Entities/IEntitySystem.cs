using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Update-based Entity system.
/// You can implement this interface in order to modify entities on a per-frame basis.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystemUpdate
{
    void Startup();

    void Shutdown();

    void Update(float deltaTime);
}

/// <summary>
/// Fixed Update-based Entity system.
/// You can implement this interface in order to modify entities in a fixed time step.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystemFixedUpdate
{
    void Startup();

    void Shutdown();

    void FixedUpdate(float deltaTime);
}
