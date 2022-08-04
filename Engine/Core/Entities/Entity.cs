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

        internal bool TryGetComponent<T>(out T component) where T: Component
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

        internal IEnumerable<T> GetComponents<T>() where T : Component
        {
            foreach (var item in components)
            {
                if (item is T outValue)
                {
                    yield return outValue;
                }
            }
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

        public T AddComponent<T>() where T: Component
        {
            try
            {
                var attributes = Attribute.GetCustomAttributes(typeof(T));

                foreach(var attribute in attributes)
                {
                    if(attribute is DisallowMultipleComponentAttribute && components.Any(x => x is T))
                    {
                        //TODO: Log

                        return null;
                    }
                }

                var constructor = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

                var component = (T)constructor.Invoke(new object[] { this });

                if (component == null)
                {
                    return null;
                }

                components.Add(component);

                return component;
            }
            catch (Exception e)
            {
                //TODO: Log

                return null;
            }
        }
    }
}
