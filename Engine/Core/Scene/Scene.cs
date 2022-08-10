using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if _DEBUG
[assembly: InternalsVisibleTo("CoreTests")]
#endif

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple
{
    public class Scene
    {
        internal List<Entity> entities = new List<Entity>();

        public static Scene current { get; internal set; }

        internal static List<string> sceneList = new List<string>();

        public void Load(string path)
        {
            var scene = ResourceManager.instance.LoadScene(path);

            if(scene == null)
            {
                return;
            }

            current?.Cleanup();

            current = scene;
        }

        public Entity Find(string name)
        {
            return entities.FirstOrDefault(x => x.Name == name);
        }

        public Entity FindID(string ID)
        {
            return entities.FirstOrDefault(x => x.ID == ID);
        }

        internal void Cleanup()
        {
            foreach(var entity in entities)
            {
                foreach(var component in entity.components)
                {
                    component?.Invoke("OnDestroy");
                }
            }
        }

        public IEnumerable<T> GetComponents<T>() where T: Component
        {
            foreach(var entity in entities)
            {
                var components = entity.GetComponents<T>();

                foreach(var item in components)
                {
                    yield return item;
                }
            }
        }

        internal void AddEntity(Entity entity)
        {
            if(entities.Contains(entity) == false)
            {
                entities.Add(entity);
            }
        }

        internal void RemoveEntity(Entity entity)
        {
            if(entity != null)
            {
                foreach (var component in entity.components)
                {
                    component?.Invoke("OnDestroy");
                }
            }

            entities.Remove(entity);
        }
    }
}
