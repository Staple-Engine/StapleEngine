using System;

namespace Staple;

/// <summary>
/// Render System Interface
/// </summary>
internal interface IRenderSystem
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
    /// <param name="world">The world the entity belongs to</param>
    /// <param name="entity">The entity</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    void Preprocess(World world, Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform);

    /// <summary>
    /// Processes the entity.
    /// This is when you should handle the entity's data in order to render.
    /// </summary>
    /// <param name="world">The world the entity belongs to</param>
    /// <param name="entity">The entity</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="viewId">The current view ID</param>
    void Process(World world, Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera,
        Transform activeCameraTransform, ushort viewId);

    /// <summary>
    /// Submits all rendering commands to the renderer.
    /// </summary>
    void Submit();

    /// <summary>
    /// Destroys this render system.
    /// </summary>
    void Destroy();
}
