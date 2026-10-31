using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    /// Creates a new instance of this render system's render data
    /// </summary>
    /// <returns>A new render data</returns>
    public abstract IRenderData CreateRenderData();

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
    /// Processes a render queue.
    /// This is when you should handle the queue's data in order to render.
    /// </summary>
    /// <param name="renderQueue">A list of all renderables</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="renderIndex">The <see cref="Material.RenderQueueIndex"/> for the material that should be renderered.
    /// Use this to ensure you're rendering the proper render queue from each material you process</param>
    public abstract void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex);

    /// <summary>
    /// Prepares the render data for this render system.
    /// Use this to prepare all render data as if all objects should be visible.
    /// You likely want to keep track of your renderers in the render data as well.
    /// </summary>
    /// <param name="renderData">The render data to prepare</param>
    /// <param name="renderQueue">A list of all renderables</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    /// <param name="renderIndex">The <see cref="Material.RenderQueueIndex"/> for the material that should be renderered.
    /// Use this to ensure you're rendering the proper render queue from each material you process</param>
    public abstract void PrepareRenderData(IRenderData renderData, IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform,
        int renderIndex);

    /// <summary>
    /// Updates a render data's visiblity info.
    /// Use this to update only whether an object should be rendered or not
    /// </summary>
    /// <param name="renderData">The render data to update</param>
    /// <param name="activeCamera">The current active camera</param>
    /// <param name="activeCameraTransform">The current active camera's transform</param>
    public abstract void UpdateRenderDataVisibility(IRenderData renderData, Camera activeCamera, Transform activeCameraTransform);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool IsValidMaterial(Material material, int renderIndex)
    {
        return (material?.RenderQueueIndex ?? -1) == renderIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void IterateValidMaterials(Renderable renderable, int renderIndex, Action<int> callback)
    {
        if ((renderable?.materials?.Count ?? 0) == 0)
        {
            return;
        }

        for (var i = 0; i < renderable.materials.Count; i++)
        {
            if ((renderable.materials[i]?.RenderQueueIndex ?? -1) != renderIndex)
            {
                continue;
            }

            callback?.Invoke(i);
        }
    }
}
