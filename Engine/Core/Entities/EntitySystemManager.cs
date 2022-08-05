using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class EntitySystemManager : ISubsystem
    {
        private HashSet<IEntitySystem> systems = new HashSet<IEntitySystem>();

        public static readonly EntitySystemManager instance = new EntitySystemManager();

        internal static readonly byte Priority = 1;

        public IEnumerable<Entity> FindEntities(params Type[] types)
        {
            var entities = Scene.current.entities;

            foreach (var entity in entities)
            {
                if (entity?.HasComponents(types) ?? false)
                {
                    yield return entity;
                }
            }
        }

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
            foreach(var system in systems)
            {
                if((system.targetComponents?.Length ?? 0) == 0)
                {
                    continue;
                }

                var entities = FindEntities(system.targetComponents);

                foreach(var entity in entities)
                {
                    system.Process(entity);
                }
            }
        }
    }
}
