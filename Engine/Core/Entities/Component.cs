using System;
using System.Reflection;

namespace Staple
{
    public class Component
    {
        public WeakReference<Entity> Entity { get; internal set; }

        public Transform Transform
        {
            get
            {
                if(Entity != null && Entity.TryGetTarget(out var entity))
                {
                    return entity.Transform;
                }

                return null;
            }
        }

        internal void Invoke(string name)
        {
            const BindingFlags flags = BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            MethodInfo method = GetType().GetMethod(name, flags);

            try
            {
                if (method != null && method.GetParameters().Length == 0)
                {
                    method.Invoke(this, new object[0]);
                }
            }
            catch(Exception)
            {
            }
        }
    }
}