using System.Collections.Generic;
using System.Reflection;

namespace Staple;

/// <summary>
/// Manages entity systems
/// The player will automatically register entity systems.
/// </summary>
internal class EntitySystemManager : ISubsystem
{
    private static readonly Dictionary<SubsystemType, EntitySystemManager> entitySubsystems = new();

    internal static readonly byte Priority = 0;

    private SubsystemType timing = SubsystemType.FixedUpdate;

    public SubsystemType type => timing;

    private readonly HashSet<IEntitySystem> systems = new();

    /// <summary>
    /// Gets the entity system manager for a specific subsystem type
    /// </summary>
    /// <param name="type">The subsystem type</param>
    /// <returns>The entity subsystem manager, or null</returns>
    public static EntitySystemManager GetEntitySystem(SubsystemType type)
    {
        if(entitySubsystems.Count == 0)
        {
            entitySubsystems.Add(SubsystemType.FixedUpdate, new EntitySystemManager()
            {
                timing = SubsystemType.FixedUpdate,
            });

            entitySubsystems.Add(SubsystemType.Update, new EntitySystemManager()
            {
                timing = SubsystemType.Update,
            });
        }

        return entitySubsystems.TryGetValue(type, out var manager) ? manager : null;
    }

    /// <summary>
    /// Finds all entity systems subclassing or implementing a specific type
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <returns>All entity systems currently loaded of that type</returns>
    public static T[] FindEntitySystemsSubclassing<T>()
    {
        var outValue = new List<T>();

        foreach(var pair in entitySubsystems)
        {
            foreach(var system in pair.Value.systems)
            {
                if(system.GetType().IsSubclassOf(typeof(T)) ||
                    system.GetType().IsAssignableTo(typeof(T)))
                {
                    outValue.Add((T)system);
                }
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

    public void Update()
    {
        if(Scene.current == null)
        {
            return;
        }

        var time = timing switch
        {
            SubsystemType.FixedUpdate => Time.fixedDeltaTime,
            _ => Time.deltaTime,
        };

        foreach (var system in systems)
        {
            system.Process(time);
        }
    }
}
