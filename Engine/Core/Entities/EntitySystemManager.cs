using System.Collections.Generic;
using System.Reflection;

namespace Staple;

/// <summary>
/// Manages entity systems
/// The player will automatically register entity systems.
/// </summary>
internal class EntitySystemManager : ISubsystem
{
    internal static readonly byte Priority = 0;

    public SubsystemType type => SubsystemType.Update;

    private readonly HashSet<IEntitySystem> systems = new();

    public static readonly EntitySystemManager Instance = new();

    /// <summary>
    /// Finds all entity systems subclassing or implementing a specific type
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <returns>All entity systems currently loaded of that type</returns>
    public T[] FindEntitySystemsSubclassing<T>()
    {
        var outValue = new List<T>();

        foreach(var system in systems)
        {
            if(system.GetType().IsSubclassOf(typeof(T)) ||
                system.GetType().IsAssignableTo(typeof(T)))
            {
                outValue.Add((T)system);
            }
        }

        return outValue.ToArray();
    }

    /// <summary>
    /// Unloads all entity subsystems that belong to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to unload from</param>
    internal void UnloadSystemsFromAssembly(Assembly assembly)
    {
        var unloaded = new List<IEntitySystem>();

        foreach(var system in systems)
        {
            if(system.GetType().Assembly == assembly)
            {
                unloaded.Add(system);
            }
        }

        foreach(var system in unloaded)
        {
            system.Shutdown();

            systems.Remove(system);
        }
    }

    /// <summary>
    /// Registers an entity system.
    /// </summary>
    /// <param name="system">The system to register</param>
    public void RegisterSystem(IEntitySystem system)
    {
        systems.Add(system);

        system.Startup();
    }

    public void Shutdown()
    {
        foreach(var system in systems)
        {
            system.Shutdown();
        }

        systems.Clear();
    }

    public void Startup()
    {
    }

    public void UpdateFixed()
    {
        if (Scene.current == null)
        {
            return;
        }

        var time = Time.fixedDeltaTime;

        foreach (var system in systems)
        {
            if (system.UpdateType == EntitySubsystemType.FixedUpdate || system.UpdateType == EntitySubsystemType.Both)
            {
                system.FixedUpdate(time);
            }
        }
    }

    public void Update()
    {
        if(Scene.current == null)
        {
            return;
        }

        var time = Time.deltaTime;

        foreach (var system in systems)
        {
            if(system.UpdateType == EntitySubsystemType.Update || system.UpdateType == EntitySubsystemType.Both)
            {
                system.Update(time);
            }
        }
    }
}
