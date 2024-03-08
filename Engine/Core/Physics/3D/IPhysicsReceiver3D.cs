namespace Staple;

/// <summary>
/// Interface for entity systems that handle physics events.
/// </summary>
public interface IPhysicsReceiver3D
{
    /// <summary>
    /// Event for when a body is activated
    /// </summary>
    /// <param name="body">The body that was activated</param>
    void OnBodyActivated(IBody3D body);

    /// <summary>
    /// Event for when a body is deactivated
    /// </summary>
    /// <param name="body">The body that was deactivated</param>
    void OnBodyDeactivated(IBody3D body);

    /// <summary>
    /// Event for when a body just collided with another
    /// </summary>
    /// <param name="A">The first body that collided</param>
    /// <param name="B">The second body that collided</param>
    /// <remarks>There's no repeat events for the other body with the parameters swapped, so you should check both bodies</remarks>
    void OnContactAdded(IBody3D A, IBody3D B);

    /// <summary>
    /// Event for when a body is still colliding with another
    /// </summary>
    /// <param name="A">The first body that collided</param>
    /// <param name="B">The second body that collided</param>
    /// <remarks>There's no repeat events for the other body with the parameters swapped, so you should check both bodies</remarks>
    void OnContactPersisted(IBody3D A, IBody3D B);

    /// <summary>
    /// Event for checking whether a body should collide with another
    /// </summary>
    /// <param name="A">The first body that collided</param>
    /// <param name="B">The second body that collided</param>
    /// <remarks>There's no repeat events for the other body with the parameters swapped, so you should check both bodies</remarks>
    bool OnContactValidate(IBody3D A, IBody3D B);
}
