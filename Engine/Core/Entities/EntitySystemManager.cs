using System.Collections.Generic;

namespace Staple
{
    /// <summary>
    /// Manages entity systems
    /// The player will automatically register entity systems.
    /// </summary>
    internal class EntitySystemManager : ISubsystem
    {
        private static readonly Dictionary<SubsystemType, EntitySystemManager> entitySubsystems = new();

        internal static readonly byte Priority = 1;

        private SubsystemType timing = SubsystemType.FixedUpdate;

        public SubsystemType type => timing;

        private readonly HashSet<IEntitySystem> systems = new();

        public static EntitySystemManager GetEntitySystem(SubsystemType type)
        {
            if(entitySubsystems.Count == 0)
            {
                entitySubsystems.Add(SubsystemType.Render, new EntitySystemManager()
                {
                    timing = SubsystemType.Render,
                });

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

        /// <summary>
        /// Registers an entity system.
        /// </summary>
        /// <param name="system">The system to register</param>
        public void RegisterSystem(IEntitySystem system)
        {
            systems.Add(system);
        }

        public void Shutdown()
        {
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

            float time;

            switch (timing)
            {
                case SubsystemType.FixedUpdate:

                    time = Time.fixedDeltaTime;

                    break;

                default:

                    time = Time.deltaTime;

                    break;
            }

            foreach (var system in systems)
            {
                system.Process(Scene.current.world, time);
            }
        }
    }
}
