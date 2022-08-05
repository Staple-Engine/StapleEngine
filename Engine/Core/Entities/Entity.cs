using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#if _DEBUG
[assembly: InternalsVisibleTo("CoreTests")]
#endif

namespace Staple
{
    public class Entity
    {
        internal List<Component> components = new List<Component>();

        public readonly string Name;

        public readonly Transform Transform;

        public uint layer;

        public Entity(string name)
        {
            Name = name;
            Transform = new Transform(this);

            Scene.current?.AddEntity(this);
        }

        ~Entity()
        {
            Scene.current?.RemoveEntity(this);
        }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = null;

            foreach(var item in components)
            {
                if(item is T outValue)
                {
                    component = outValue;

                    return true;
                }
            }

            return false;
        }

        public IEnumerable<T> GetComponents<T>() where T : Component
        {
            foreach (var item in components)
            {
                if (item != null && item.GetType() == typeof(T))
                {
                    yield return (T)item;
                }
            }
        }

        public T GetComponent<T>() where T : Component
        {
            foreach(var item in components)
            {
                if(item != null && item.GetType() == typeof(T))
                {
                    return (T)item;
                }
            }

            return null;
        }

        internal bool HasComponents(params Type[] types)
        {
            for(var i = 0; i < types.Length; i++)
            {
                if (types[i].IsSubclassOf(typeof(Component)) == false)
                {
                    return false;
                }
            }

            for(var i = 0; i < types.Length; i++)
            {
                bool found = false;

                for(var j = 0; j < components.Count; j++)
                {
                    if (components[j] == null)
                    {
                        continue;
                    }

                    var type = components[j].GetType();

                    if (type == types[i] ||
                        type.IsSubclassOf(types[j]) ||
                        types[j].IsAssignableFrom(type))
                    {
                        found = true;

                        break;
                    }
                }

                if (found == false)
                {
                    return false;
                }
            }

            return true;
        }

        public Component AddComponent(Type t)
        {
            if(t.IsSubclassOf(typeof(Component)) == false)
            {
                return null;
            }

            try
            {
                var attributes = Attribute.GetCustomAttributes(t);

                foreach (var attribute in attributes)
                {
                    if (attribute is DisallowMultipleComponentAttribute && components.Any(x => x.GetType() == t))
                    {
                        //TODO: Log

                        return null;
                    }
                }

                var component = (Component)Activator.CreateInstance(t);

                if (component == null)
                {
                    return null;
                }

                component.Entity = new WeakReference<Entity>(this);

                components.Add(component);

                return component;
            }
            catch (Exception e)
            {
                //TODO: Log

                return null;
            }

        }

        public T AddComponent<T>() where T: Component
        {
            return (T)AddComponent(typeof(T));
        }
    }
}
