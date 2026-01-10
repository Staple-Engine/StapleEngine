using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Manages subsystems. This uses a priority order.
/// </summary>
internal class SubsystemManager
{
    /// <summary>
    /// The main instance
    /// </summary>
    public static readonly SubsystemManager instance = new();

    private bool needsRecalculation = true;

    private readonly SortedDictionary<byte, HashSet<ISubsystem>> subsystems = [];
    private readonly ExpandableContainer<ISubsystem> collapsedSystems = new();

    private readonly Lock lockObject = new();

    /// <summary>
    /// Destroys each subsystem
    /// </summary>
    internal void Destroy()
    {
        lock (lockObject)
        {
            //Shutdown in reverse order
            foreach (var key in subsystems.Keys.Reverse())
            {
                foreach (var subsystem in subsystems[key])
                {
                    subsystem?.Shutdown();
                }
            }
        }
    }

    /// <summary>
    /// Registers a subsystem based on a priority
    /// </summary>
    /// <param name="subsystem">The subsystem</param>
    /// <param name="priority">The priority</param>
    public void RegisterSubsystem(ISubsystem subsystem, byte priority)
    {
        lock (lockObject)
        {
            if (!subsystems.TryGetValue(priority, out var list))
            {
                list = [];

                subsystems.Add(priority, list);
            }

            list.Add(subsystem);

            subsystem.Startup();

            if (subsystem is IWorldChangeReceiver worldChangeReceiver)
            {
                World.AddChangeReceiver(worldChangeReceiver);
            }

            needsRecalculation = true;
        }
    }

    /// <summary>
    /// Called to update each subsystem of a specific type
    /// </summary>
    /// <param name="type"></param>
    internal void Update(SubsystemType type)
    {
        if(needsRecalculation)
        {
            needsRecalculation = false;

            lock (lockObject)
            {
                collapsedSystems.Clear();

                foreach (var pair in subsystems)
                {
                    foreach (var subsystem in pair.Value)
                    {
                        collapsedSystems.Add(subsystem);
                    }
                }
            }
        }

        lock (lockObject)
        {
            foreach (var subsystem in collapsedSystems.Contents)
            {
                if (subsystem.type != type)
                {
                    continue;
                }

                subsystem.Update();
            }
        }
    }
}
