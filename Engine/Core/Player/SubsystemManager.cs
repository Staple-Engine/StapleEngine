using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class SubsystemManager
    {
        public static readonly SubsystemManager instance = new SubsystemManager();

        private SortedDictionary<byte, HashSet<ISubsystem>> subsystems = new SortedDictionary<byte, HashSet<ISubsystem>>();
        private SortedDictionary<byte, HashSet<ISubsystem>> fixedUpdateSubsystems = new SortedDictionary<byte, HashSet<ISubsystem>>();
        private SortedDictionary<byte, HashSet<ISubsystem>> renderSubsystems = new SortedDictionary<byte, HashSet<ISubsystem>>();
        private object lockObject = new object();

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

                foreach (var key in fixedUpdateSubsystems.Keys.Reverse())
                {
                    foreach (var subsystem in fixedUpdateSubsystems[key])
                    {
                        subsystem?.Shutdown();
                    }
                }

                foreach (var key in renderSubsystems.Keys.Reverse())
                {
                    foreach (var subsystem in renderSubsystems[key])
                    {
                        subsystem?.Shutdown();
                    }
                }
            }
        }

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

        public void RegisterFixedSubsystem(ISubsystem subsystem, byte priority)
        {
            lock (lockObject)
            {
                if (fixedUpdateSubsystems.TryGetValue(priority, out var list) == false)
                {
                    list = new HashSet<ISubsystem>();

                    fixedUpdateSubsystems.Add(priority, list);
                }

                list.Add(subsystem);

                subsystem.Startup();
            }
        }

        public void RegisterRenderSubsystem(ISubsystem subsystem, byte priority)
        {
            lock (lockObject)
            {
                if (renderSubsystems.TryGetValue(priority, out var list) == false)
                {
                    list = new HashSet<ISubsystem>();

                    renderSubsystems.Add(priority, list);
                }

                list.Add(subsystem);

                subsystem.Startup();
            }
        }

        internal void Update()
        {
            lock (lockObject)
            {
                foreach (var pair in subsystems)
                {
                    foreach (var subsystem in pair.Value)
                    {
                        subsystem.Update();
                    }
                }
            }
        }

        internal void FixedUpdate()
        {
            lock (lockObject)
            {
                foreach (var pair in fixedUpdateSubsystems)
                {
                    foreach (var subsystem in pair.Value)
                    {
                        subsystem.Update();
                    }
                }
            }
        }

        internal void Render()
        {
            lock (lockObject)
            {
                foreach (var pair in renderSubsystems)
                {
                    foreach (var subsystem in pair.Value)
                    {
                        subsystem.Update();
                    }
                }
            }
        }
    }
}
