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
    /// It will be rendered after all other render systems.
    /// </summary>
    bool UsesOwnRenderProcess { get; }

    /// <summary>
    /// The type of the component that this render system uses
    /// </summary>
    Type RelatedComponent { get; }

    /// <summary>
    /// Prepares the render system for rendering.
    /// Called before entities are processed.
    /// </summary>
    void Prepare();

    /// <summary>
    /// Pre-processes an entity.
    /// Use this to prepare information before the rendering pass, such as updating bounds.
    /// </summary>
    /// <param name="renderQueue">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform);

    /// <summary>
    /// Processes the entity.
    /// This is when you should handle the entity's data in order to render.
    /// </summary>
    /// <param name="renderQueue">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform);

    /// <summary>
    /// Submits all rendering commands to the renderer
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
