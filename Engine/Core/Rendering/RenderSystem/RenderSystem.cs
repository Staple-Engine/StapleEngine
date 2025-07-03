using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

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

    /// <summary>
    /// Contains information on a draw call
    /// </summary>
    internal class DrawCall
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Entity entity;
        public Renderable renderable;
        public IComponent relatedComponent;
    }

    /// <summary>
    /// Contains lists of drawcalls per view ID
    /// </summary>
    internal class DrawBucket
    {
        public Dictionary<ushort, List<DrawCall>> drawCalls = [];
    }

    public SubsystemType type { get; } = SubsystemType.Update;

    internal static byte Priority = 1;

    /// <summary>
    /// Whether to use a drawcall interpolator. Can allow for small ticks of updates causing smooth rendering.
    /// </summary>
    public static bool UseDrawcallInterpolator = false;

    /// <summary>
    /// The amount of renderers that were culled
    /// </summary>
    public static int CulledRenderers { get; internal set; }

    /// <summary>
    /// The current view ID that is being rendered
    /// </summary>
    public static ushort CurrentViewID { get; private set; }

    /// <summary>
    /// The current camera
    /// </summary>
    public static (Camera, Transform) CurrentCamera { get; internal set; }

    /// <summary>
    /// The instance of this render system
    /// </summary>
    public static readonly RenderSystem Instance = new();

    /// <summary>
    /// Keep the current and previous draw buckets to interpolate around
    /// </summary>
    private DrawBucket previousDrawBucket = new(), currentDrawBucket = new();

    /// <summary>
    /// Render thread lock
    /// </summary>
    private readonly Lock lockObject = new();

    /// <summary>
    /// The frustum culler to use with a camera
    /// </summary>
    private readonly FrustumCuller frustumCuller = new();

    /// <summary>
    /// Whether we need to generate draw calls (interpolator only)
    /// </summary>
    private bool needsDrawCalls = false;

    /// <summary>
    /// Time accumulator (interpolator only)
    /// </summary>
    private float accumulator = 0.0f;

    /// <summary>
    /// All registered render systems
    /// </summary>
    internal readonly List<IRenderSystem> renderSystems = [];

    /// <summary>
    /// Temporary transform for rendering with the interpolator
    /// </summary>
    private readonly Transform stagingTransform = new();

    /// <summary>
    /// Queued list of callbacks for frames
    /// </summary>
    private readonly Dictionary<uint, List<Action>> queuedFrameCallbacks = [];

    /// <summary>
    /// The render queue
    /// </summary>
    private readonly List<((Camera, Transform), List<(IRenderSystem, (Entity, Transform, IComponent)[])>)> renderQueue = [];

    /// <summary>
    /// The entity query for every entity with a transform
    /// </summary>
    private readonly SceneQuery<Transform> entityQuery = new();

    /// <summary>
    /// Cached per-frame used view IDs
    /// </summary>
    private HashSet<ushort> usedViewIDs = [];

    /// <summary>
    /// Cached per-frame used view IDs (previous frame)
    /// </summary>
    private HashSet<ushort> previousUsedViewIDs = [];

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
}
