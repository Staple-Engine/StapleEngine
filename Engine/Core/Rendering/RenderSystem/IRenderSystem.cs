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
    /// Whether this render system uses its own render process.
    /// It will not be given its own view ID and should ignore the <see cref="Process"/> and <see cref="Submit(ushort)"/> viewID parameter.
    /// </summary>
    bool UsesOwnRenderProcess { get; }

    /// <summary>
    /// The type of the component that this render system uses
    /// </summary>
    Type RelatedComponent { get; }

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
    void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform);

    /// <summary>
    /// Processes the entity.
    /// This is when you should handle the entity's data in order to render.
    /// </summary>
    /// <param name="entities">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="viewID">The current view ID</param>
    void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID);

    /// <summary>
    /// Submits all rendering commands for a specific view to the renderer
    /// </summary>
    /// <param name="viewID">The view ID</param>
    void Submit(ushort viewID);

    /// <summary>
    /// Instructs the render system to clear any cached render data for a specific view ID
    /// </summary>
    /// <param name="viewID">The view ID</param>
    void ClearRenderData(ushort viewID);

    /// <summary>
    /// Called when this render system starts up
    /// </summary>
    void Startup();

    /// <summary>
    /// Called when this render system shuts down
    /// </summary>
    void Shutdown();
}
