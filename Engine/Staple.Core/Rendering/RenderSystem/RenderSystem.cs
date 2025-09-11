using System;

namespace Staple.Internal;

/// <summary>
/// Rendering subsystem, handles all rendering
/// </summary>
[AdditionalLibrary(AppPlatform.Android, "bgfx")]
public sealed partial class RenderSystem : ISubsystem, IWorldChangeReceiver
{
    /// <summary>
    /// The ID of the first camera in the scene
    /// </summary>
    public const ushort FirstCameraViewID = 1;

    /// <summary>
    /// The ID of the editor scene view
    /// </summary>
    public const ushort EditorSceneViewID = 253;

    public SubsystemType type { get; } = SubsystemType.Update;

    /// <summary>
    /// Whether to use a drawcall interpolator. Can allow for small ticks of updates causing smooth rendering.
    /// </summary>
    public static bool UseDrawcallInterpolator = false;

    /// <summary>
    /// Rendering statistics
    /// </summary>
    public static readonly RenderStats RenderStats = new();

    /// <summary>
    /// The current view ID that is being rendered
    /// </summary>
    public static ushort CurrentViewID { get; private set; }

    /// <summary>
    /// The current frame being rendered
    /// </summary>
    public static uint CurrentFrame { get; private set; }

    /// <summary>
    /// The current camera
    /// </summary>
    public static (Camera, Transform) CurrentCamera { get; internal set; }

    /// <summary>
    /// The instance of this render system
    /// </summary>
    public static readonly RenderSystem Instance = new();

    /// <summary>
    /// Registers a render system into this subsystem
    /// </summary>
    /// <param name="system">The system to add</param>
    public void RegisterSystem(IRenderSystem system)
    {
        lock (lockObject)
        {
            foreach (var s in renderSystems)
            {
                if (s == system || s.GetType() == system.GetType())
                {
                    return;
                }
            }

            try
            {
                system.Startup();
            }
            catch (Exception e)
            {
                Log.Error($"[RenderSystem] Failed to initialize {system.GetType().FullName}: {e}");

                return;
            }

            if (system is IWorldChangeReceiver receiver)
            {
                World.AddChangeReceiver(receiver);
            }

            renderSystems.Add(system);
        }
    }

    /// <summary>
    /// Gets a registered render system. This render system must have been registered previously.
    /// </summary>
    /// <typeparam name="T">The render system type</typeparam>
    /// <returns>The system, or default</returns>
    public T Get<T>() where T: IRenderSystem
    {
        lock(lockObject)
        {
            foreach(var s in renderSystems)
            {
                if(s is T instance)
                {
                    return instance;
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Clears the queued render data for a specific view ID
    /// </summary>
    /// <param name="viewID">The view ID to clear</param>
    public void ClearRenderData(ushort viewID)
    {
        lock (lockObject)
        {
            foreach (var s in renderSystems)
            {
                s.ClearRenderData(viewID);
            }
        }
    }
}
