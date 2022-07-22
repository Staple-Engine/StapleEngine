using System;

namespace Staple
{
    public class Component
    {
        public WeakReference<Entity> Entity { get; private set; }

        public Transform Transform => (Entity?.TryGetTarget(out var entity) ?? false) ? entity.Transform : null;

        internal Component(Entity entity)
        {
            Entity = new WeakReference<Entity>(entity);
        }
    }
}