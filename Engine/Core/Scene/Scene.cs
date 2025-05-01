using System;
using System.Collections.Generic;

namespace Staple;

public class Scene
{
    internal static bool InstancingComponent = false;

    /// <summary>
    /// The currently active scene
    /// </summary>
    internal static Scene current { get; set; }

    /// <summary>
    /// A list of all scenes we can load
    /// </summary>
    internal static List<string> sceneList = [];

    /// <summary>
    /// Gets all available cameras sorted by depth
    /// </summary>
    public static World.CameraInfo[] SortedCameras => World.Current?.SortedCameras ?? [];

    /// <summary>
    /// Requests a world update.
    /// Use when you're doing changes that the world should be aware of that require an update.
    /// Usually you won't need to use this.
    /// </summary>
    public static void RequestWorldUpdate()
    {
        World.Current?.RequestWorldUpdate();
    }

    /// <summary>
    /// Changes the currently active scene
    /// </summary>
    /// <param name="scene">The new scene</param>
    internal static void SetActiveScene(Scene scene)
    {
        current = scene;
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public static (Entity, T)[] Query<T>(bool includeDisabled = false) where T : IComponent
    {
        return World.Current?.Query<T>(includeDisabled);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public static (Entity, T, T2)[] Query<T, T2>(bool includeDisabled = false)
        where T : IComponent
        where T2 : IComponent
    {
        return World.Current?.Query<T, T2>(includeDisabled);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public static (Entity, T, T2, T3)[] Query<T, T2, T3>(bool includeDisabled = false)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        return World.Current?.Query<T, T2, T3>(includeDisabled);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public static (Entity, T, T2, T3, T4)[] Query<T, T2, T3, T4>(bool includeDisabled = false)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        return World.Current?.Query<T, T2, T3, T4>(includeDisabled);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <typeparam name="T5">The type of the fifth component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public static (Entity, T, T2, T3, T4, T5)[] Query<T, T2, T3, T4, T5>(bool includeDisabled = false)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        return World.Current?.Query<T, T2, T3, T4, T5>(includeDisabled);
    }

    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    /// <param name="ID">The entity's ID</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public static Entity FindEntity(int ID)
    {
        return World.Current?.FindEntity(ID) ?? default;
    }

    /// <summary>
    /// Attempts to find an entity by name
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public static Entity FindEntity(string name, bool allowDisabled = false)
    {
        return World.Current?.FindEntity(name, allowDisabled) ?? default;
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="componentType">The component's type</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public static bool TryFindEntityComponent(string name, bool allowDisabled, Type componentType, out IComponent component)
    {
        if(World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryFindEntityComponent(name, allowDisabled, componentType, out component);
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public static bool TryFindEntityComponent<T>(string name, bool allowDisabled, out T component) where T : IComponent
    {
        if (World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryFindEntityComponent(name, allowDisabled, out component);
    }

    /// <summary>
    /// Iterates through each entity in the scene/world
    /// </summary>
    /// <param name="callback">A callback to handle an entity</param>
    public static void IterateEntities(Action<Entity> callback)
    {
        World.Current?.Iterate(callback);
    }
}
