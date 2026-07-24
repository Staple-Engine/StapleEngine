using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Render System base class
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class RenderSystemBase(bool usesOwnRenderProcess, Type relatedComponent, Type queueType)
{
    /// <summary>
    /// Whether this render system uses its own render process.
    /// It will be rendered after all other render systems.
    /// </summary>
    public readonly bool UsesOwnRenderProcess = usesOwnRenderProcess;

    /// <summary>
    /// The type of the component that this render system uses
    /// </summary>
    public readonly Type RelatedComponent = relatedComponent;

    /// <summary>
    /// The type of the expected render queue
    /// </summary>
    public readonly Type QueueType = queueType;

    /// <summary>
    /// Creates a new instance of this render system's render queue
    /// </summary>
    /// <returns>A new render queue</returns>
    public abstract IRenderQueue CreateRenderQueue();

    /// <summary>
    /// Prepares the render system for rendering.
    /// Called before entities are processed.
    /// </summary>
    public abstract void Prepare();

    /// <summary>
    /// Pre-processes an entity.
    /// Use this to prepare information before the rendering pass, such as updating bounds.
    /// </summary>
    /// <param name="renderQueue">A list of all entities, transforms, and the related component</param>
    public abstract void Preprocess(IRenderQueue renderQueue);

    /// <summary>
    /// Processes the entity.
    /// This is when you should handle the entity's data in order to render.
    /// </summary>
    /// <param name="renderQueue">A list of all entities, transforms, and the related component</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="renderIndex">The <see cref="Material.RenderQueueIndex"/> for the material that should be renderered.
    /// Use this to ensure you're rendering the proper render queue from each material you process</param>
    public abstract void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex);

    /// <summary>
    /// Submits all rendering commands to the renderer
    /// </summary>
    public abstract void Submit();

    /// <summary>
    /// Called when this render system starts up
    /// </summary>
    public abstract void Startup();

    /// <summary>
    /// Called when this render system shuts down
    /// </summary>
    public abstract void Shutdown();

    protected static bool IsValidMaterial(Material material, int renderIndex)
    {
        return (material?.IsValid ?? false) && material.RenderQueueIndex == renderIndex;
    }

    protected static void IterateValidMaterials(Renderable renderable, int renderIndex, Action<int> callback)
    {
        if ((renderable?.materials?.Count ?? 0) == 0)
        {
            return;
        }

        for (var i = 0; i < renderable.materials.Count; i++)
        {
            if (!IsValidMaterial(renderable.materials[i], renderIndex))
            {
                continue;
            }

            callback?.Invoke(i);
        }
    }
}
