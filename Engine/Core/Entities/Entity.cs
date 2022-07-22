using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Entity
    {
        internal List<Component> components = new List<Component>();

        public readonly string Name;

        public readonly Transform Transform;

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

        public T AddComponent<T>() where T: Component
        {
            try
            {
                var constructor = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

                var component = (T)constructor.Invoke(new Object[] { this });

                if (component == null)
                {
                    return null;
                }

                components.Add(component);

                return component;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
