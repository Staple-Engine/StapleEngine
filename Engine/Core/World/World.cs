using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Contains and manages entities
/// </summary>
public partial class World
{
    public delegate void ForEachCallback<T>(Entity entity, bool enabled, ref T a) where T : IComponent;

    public delegate void ForEachCallback<T, T2>(Entity entity, bool enabled, ref T a, ref T2 b)
        where T : IComponent
        where T2 : IComponent;

    public delegate void ForEachCallback<T, T2, T3>(Entity entity, bool enabled, ref T a, ref T2 b, ref T3 c)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent;

    public delegate void ForEachCallback<T, T2, T3, T4>(Entity entity, bool enabled, ref T a, ref T2 b, ref T3 c, ref T4 d)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent;

    public delegate void ForEachCallback<T, T2, T3, T4, T5>(Entity entity, bool enabled, ref T a, ref T2 b, ref T3 c, ref T4 d, ref T5 e)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent;

    public delegate void IterateComponentCallback(ref IComponent component);

    public delegate void OnComponentChangedCallback(World world, Entity entity, Transform transform, ref IComponent component);

    /// <summary>
    /// Contains data on an entity
    /// </summary>
    private struct EntityInfo
    {
        /// <summary>
        /// The entity's ID
        /// </summary>
        public int ID;

        /// <summary>
        /// The entity's generation
        /// </summary>
        public int generation;

        /// <summary>
        /// Whether this entity is alive. If it's not, queries on it will fail.
        /// </summary>
        public bool alive;

        /// <summary>
        /// Whether this entity is enabled. If it's not, it will not be updated or rendered.
        /// </summary>
        public bool enabled;

        /// <summary>
        /// The active components for the entity
        /// </summary>
        public List<int> components;

        /// <summary>
        /// The entity's name
        /// </summary>
        public string name;

        /// <summary>
        /// The entity's layer. Defaults to the first layer.
        /// </summary>
        public uint layer;
    }

    /// <summary>
    /// Contains info on a component and its type
    /// </summary>
    private class ComponentInfo
    {
        /// <summary>
        /// The type to instantiate
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type type;

        /// <summary>
        /// The list of components per entity
        /// </summary>
        public List<IComponent> components = new();

        /// <summary>
        /// Attempts to add a component to the list
        /// </summary>
        /// <returns>Whether successful</returns>
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

        /// <summary>
        /// Creates an instance of the component
        /// </summary>
        /// <param name="component">The component (or default)</param>
        /// <returns>Whether successful</returns>
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

    /// <summary>
    /// Contains camera entity information
    /// Used by the SortedCameras property
    /// </summary>
    public class CameraInfo
    {
        public Entity entity;
        public Camera camera;
        public Transform transform;
    }

    private readonly object lockObject = new();
    private static readonly object globalLockObject = new();
    private bool collectionModified = false;
    private readonly List<EntityInfo> entities = new();
    private readonly Dictionary<int, ComponentInfo> componentsRepository = new();

    private static readonly Dictionary<Type, List<OnComponentChangedCallback>> componentAddedCallbacks = new();
    private static readonly Dictionary<Type, List<OnComponentChangedCallback>> componentRemovedCallbacks = new();
}
