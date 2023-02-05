using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class EntitySystemManager : ISubsystem
    {
        public SubsystemType type { get; } = SubsystemType.FixedUpdate;

        private HashSet<IEntitySystem> systems = new HashSet<IEntitySystem>();

        public static readonly EntitySystemManager instance = new EntitySystemManager();

        internal static readonly byte Priority = 1;

        public void RegisterSystem(IEntitySystem system)
        {
            systems.Add(system);
        }

        public void Shutdown()
        {
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

            switch(type)
            {
                case SubsystemType.FixedUpdate:

                    time = Time.fixedDeltaTime;

                    break;

                default:

                    time = Time.deltaTime;

                    break;
            }

            foreach(var system in systems)
            {
                system.Process(Scene.current.world, time);
            }
        }
    }
}
