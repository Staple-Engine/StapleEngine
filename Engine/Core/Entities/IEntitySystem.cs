using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// The type of entity subsystem
/// </summary>
public enum EntitySubsystemType
{
    /// <summary>
    /// Runs at the fixed tick rate
    /// </summary>
    FixedUpdate,

    /// <summary>
    /// Runs every frame
    /// </summary>
    Update,

    /// <summary>
    /// Handles both kinds of updates
    /// </summary>
    Both,
}

/// <summary>
/// Entity system.
/// You can implement this interface in order to modify entities.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IEntitySystem
{
    EntitySubsystemType UpdateType { get; }

    void Startup();

    void Update(float deltaTime);

    void FixedUpdate(float deltaTime);

    void Shutdown();
}
