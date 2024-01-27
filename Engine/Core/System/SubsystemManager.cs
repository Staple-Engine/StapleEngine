using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Internal
{
    /// <summary>
    /// Manages subsystems. This uses a priority order.
    /// </summary>
    internal class SubsystemManager
    {
        /// <summary>
        /// The main instance
        /// </summary>
        public static readonly SubsystemManager instance = new();

        private readonly SortedDictionary<byte, HashSet<ISubsystem>> subsystems = new();
        private readonly object lockObject = new();

        /// <summary>
        /// Destroys each subsystem
        /// </summary>
        internal void Destroy()
        {
            lock(lockObject)
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
            lock(lockObject)
            {
                if (subsystems.TryGetValue(priority, out var list) == false)
                {
                    list = new HashSet<ISubsystem>();

                    subsystems.Add(priority, list);
                }

                list.Add(subsystem);

                subsystem.Startup();
            }
        }

        /// <summary>
        /// Called to update each subsystem of a specific type
        /// </summary>
        /// <param name="type"></param>
        internal void Update(SubsystemType type)
        {
            lock (lockObject)
            {
                foreach (var pair in subsystems)
                {
                    foreach (var subsystem in pair.Value)
                    {
                        if(subsystem.type != type)
                        {
                            continue;
                        }

                        subsystem.Update();
                    }
                }
            }
        }
    }
}
