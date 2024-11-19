using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Render System Interface
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface IRenderSystem
{
    /// <summary>
    /// The type of the component that this render system uses
    /// </summary>
    /// <returns>The component type</returns>
    Type RelatedComponent();

    /// <summary>
    /// Prepares the render system for rendering.
    /// Called before entities are processed for the current render view.
    /// </summary>
    void Prepare();

    /// <summary>
    /// Pre-processes an entity.
    /// Use this to prepare information before the rendering pass, such as updating bounds.
    /// </summary>
    /// <param name="entities">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    void Preprocess((Entity, Transform, IComponent)[] entities,
        Camera activeCamera, Transform activeCameraTransform);

    /// <summary>
    /// Processes the entity.
    /// This is when you should handle the entity's data in order to render.
    /// </summary>
    /// <param name="entities">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="viewId">The current view ID</param>
    void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera,
        Transform activeCameraTransform, ushort viewId);

    /// <summary>
    /// Submits all rendering commands to the renderer.
    /// </summary>
    void Submit();

    /// <summary>
    /// Called when this render system starts up
    /// </summary>
    void Startup();

    /// <summary>
    /// Called when this render system shuts down
    /// </summary>
    void Shutdown();
}
