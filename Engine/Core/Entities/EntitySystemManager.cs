using System.Collections.Generic;
using System.Reflection;

namespace Staple
{
    /// <summary>
    /// Manages entity systems
    /// The player will automatically register entity systems.
    /// </summary>
    internal class EntitySystemManager : ISubsystem
    {
        private static readonly Dictionary<SubsystemType, EntitySystemManager> entitySubsystems = new();

        internal static readonly byte Priority = 0;

        private SubsystemType timing = SubsystemType.FixedUpdate;

        public SubsystemType type => timing;

        private readonly HashSet<IEntitySystem> systems = new();

        public static EntitySystemManager GetEntitySystem(SubsystemType type)
        {
            if(entitySubsystems.Count == 0)
            {
                entitySubsystems.Add(SubsystemType.FixedUpdate, new EntitySystemManager()
                {
                    timing = SubsystemType.FixedUpdate,
                });

                entitySubsystems.Add(SubsystemType.Update, new EntitySystemManager()
                {
                    timing = SubsystemType.Update,
                });
            }

            return entitySubsystems.TryGetValue(type, out var manager) ? manager : null;
        }

        internal void UnloadSystemsFromAssembly(Assembly assembly)
        {
            var unloaded = new List<IEntitySystem>();

            foreach(var system in systems)
            {
                if(system.GetType().Assembly == assembly)
                {
                    unloaded.Add(system);
                }
            }

            foreach(var system in unloaded)
            {
                system.Shutdown();

                systems.Remove(system);
            }
        }

        /// <summary>
        /// Registers an entity system.
        /// </summary>
        /// <param name="system">The system to register</param>
        public void RegisterSystem(IEntitySystem system)
        {
            systems.Add(system);

            system.Startup();
        }

        public void Shutdown()
        {
            foreach(var system in systems)
            {
                system.Shutdown();
            }

            systems.Clear();
        }

        public void Startup()
        {
        }

        public void Update()
        {
            if(Scene.current == null)
            {
                return;
            }

            var time = timing switch
            {
                SubsystemType.FixedUpdate => Time.fixedDeltaTime,
                _ => Time.deltaTime,
            };

            foreach (var system in systems)
            {
                system.Process(Scene.current.world, time);
            }
        }
    }
}
