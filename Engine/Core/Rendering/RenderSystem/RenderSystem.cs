using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Staple.Internal;

/// <summary>
/// Rendering subsystem, handles all rendering
/// </summary>
[AdditionalLibrary(AppPlatform.Android, "bgfx")]
public partial class RenderSystem : ISubsystem
{
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
        public Dictionary<ushort, List<DrawCall>> drawCalls = new();
    }

    public SubsystemType type { get; } = SubsystemType.Update;

    internal static byte Priority = 1;

    public static bool UseDrawcallInterpolator = false;

    public static ushort CurrentViewID { get; private set; }

    public static readonly RenderSystem Instance = new();

    /// <summary>
    /// Keep the current and previous draw buckets to interpolate around
    /// </summary>
    private DrawBucket previousDrawBucket = new(), currentDrawBucket = new();

    private readonly object lockObject = new();

    private readonly FrustumCuller frustumCuller = new();

    private bool needsDrawCalls = false;

    private float accumulator = 0.0f;

    internal readonly List<IRenderSystem> renderSystems = new();

    private readonly Transform stagingTransform = new();

    private readonly Dictionary<uint, List<Action>> queuedFrameCallbacks = new();

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

            renderSystems.Add(system);
        }
    }

    /// <summary>
    /// Removes all subsystems belonging to an assembly
    /// </summary>
    /// <param name="assembly">The assembly to check</param>
    internal void RemoveAllSubsystems(Assembly assembly)
    {
        lock(lockObject)
        {
            for(var i = renderSystems.Count - 1; i >= 0; i--)
            {
                if (renderSystems[i].GetType().Assembly == assembly)
                {
                    renderSystems.RemoveAt(i);
                }
            }
        }
    }
}
