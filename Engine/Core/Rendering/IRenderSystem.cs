using System;

namespace Staple
{
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
        /// <param name="renderer">The related component</param>
        void Preprocess(World world, Entity entity, Transform transform, IComponent renderer);

        /// <summary>
        /// Processes the entity.
        /// This is when you should handle the entity's data in order to render.
        /// </summary>
        /// <param name="world">The world the entity belongs to</param>
        /// <param name="entity">The entity</param>
        /// <param name="transform">The entity's transform</param>
        /// <param name="renderer">The related component</param>
        /// <param name="viewId">The current view ID</param>
        void Process(World world, Entity entity, Transform transform, IComponent renderer, ushort viewId);

        /// <summary>
        /// Submits all rendering commands to the renderer.
        /// </summary>
        void Submit();

        /// <summary>
        /// Destroys this render system.
        /// </summary>
        void Destroy();
    }
}
