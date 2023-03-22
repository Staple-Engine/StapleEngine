using System;
using System.Collections.Generic;

namespace Staple
{
    public partial class World
    {
        public delegate void ForEachCallback<T>(Entity entity, ref T a) where T : IComponent;

        public delegate void ForEachCallback<T, T2>(Entity entity, ref T a, ref T2 b)
            where T : IComponent
            where T2 : IComponent;

        public delegate void ForEachCallback<T, T2, T3>(Entity entity, ref T a, ref T2 b, ref T3 c)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent;

        public delegate void ForEachCallback<T, T2, T3, T4>(Entity entity, ref T a, ref T2 b, ref T3 c, ref T4 d)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent;

        public delegate void ForEachCallback<T, T2, T3, T4, T5>(Entity entity, ref T a, ref T2 b, ref T3 c, ref T4 d, ref T5 e)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
            where T5 : IComponent;

        public delegate void IterateComponentCallback(ref IComponent component);

        public class CameraInfo
        {
            public Entity entity;
            public Camera camera;
            public Transform transform;
        }

        private struct EntityInfo
        {
            public int ID;
            public int generation;
            public bool alive;
            public List<int> components;
            public string name;
        }

        private class ComponentInfo
        {
            public Type type;
            public List<IComponent> components = new List<IComponent>();

            public bool AddComponent()
            {
                try
                {
                    var t = (IComponent)Activator.CreateInstance(type);

                    if (t != null)
                    {
                        components.Add(t);

                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add component {type.FullName}: {e}");
                }

                return false;
            }

            public bool Create(out IComponent component)
            {
                try
                {
                    var t = (IComponent)Activator.CreateInstance(type);

                    if (t != null)
                    {
                        component = t;

                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create component {type.FullName}: {e}");
                }

                component = default;

                return false;
            }
        }

        private object lockObject = new object();
        private List<EntityInfo> entities = new List<EntityInfo>();
        private Dictionary<int, ComponentInfo> componentsRepository = new Dictionary<int, ComponentInfo>();
    }
}
