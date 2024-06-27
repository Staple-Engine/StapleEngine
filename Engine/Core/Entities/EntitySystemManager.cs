using System;
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

    private readonly HashSet<IEntitySystemUpdate> updateSystems = new();
    private readonly HashSet<IEntitySystemFixedUpdate> fixedUpdateSystems = new();

    private readonly Dictionary<string, object> cachedSubclasses = new();

    public static readonly EntitySystemManager Instance = new();

    /// <summary>
    /// Finds all entity systems subclassing or implementing a specific type
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <returns>All entity systems currently loaded of that type</returns>
    public T[] FindEntitySystemsSubclassing<T>()
    {
        if(cachedSubclasses.TryGetValue(typeof(T).FullName, out var subclasses))
        {
            return (T[])subclasses;
        }

        var outValue = new List<T>();

        foreach(var system in updateSystems)
        {
            if(system.GetType().IsSubclassOf(typeof(T)) ||
                system.GetType().IsAssignableTo(typeof(T)))
            {
                outValue.Add((T)system);
            }
        }

        foreach (var system in fixedUpdateSystems)
        {
            if (system.GetType().IsSubclassOf(typeof(T)) ||
                system.GetType().IsAssignableTo(typeof(T)))
            {
                outValue.Add((T)system);
            }
        }

        var v = outValue.ToArray();

        cachedSubclasses.Add(typeof(T).FullName, v);

        return v;
    }

    /// <summary>
    /// Unloads all entity subsystems that belong to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to unload from</param>
    internal void UnloadSystemsFromAssembly(Assembly assembly)
    {
        var unloadedUpdate = new List<IEntitySystemUpdate>();
        var unloadedFixedUpdate = new List<IEntitySystemFixedUpdate>();

        foreach (var system in updateSystems)
        {
            if(system.GetType().Assembly == assembly)
            {
                unloadedUpdate.Add(system);
            }
        }

        foreach (var system in fixedUpdateSystems)
        {
            if (system.GetType().Assembly == assembly)
            {
                unloadedFixedUpdate.Add(system);
            }
        }

        foreach (var system in unloadedUpdate)
        {
            updateSystems.Remove(system);

            system.Shutdown();
        }

        foreach(var system in unloadedFixedUpdate)
        {
            fixedUpdateSystems.Remove(system);

            var skip = false;

            foreach(var o in unloadedUpdate)
            {
                if(o == system)
                {
                    skip = true;

                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            system.Shutdown();
        }

        cachedSubclasses.Clear();
    }

    /// <summary>
    /// Registers an entity system.
    /// </summary>
    /// <param name="system">The system to register</param>
    public void RegisterSystem(object system)
    {
        var ranStartup = false;

        if(system is IEntitySystemFixedUpdate fixedUpdate)
        {
            fixedUpdateSystems.Add(fixedUpdate);

            ranStartup = true;

            fixedUpdate.Startup();
        }

        if(system is IEntitySystemUpdate update)
        {
            updateSystems.Add(update);

            if(ranStartup == false)
            {
                update.Startup();
            }
        }

        cachedSubclasses.Clear();
    }

    public void Shutdown()
    {
        foreach(var system in updateSystems)
        {
            system.Shutdown();
        }

        foreach (var system in fixedUpdateSystems)
        {
            system.Shutdown();
        }

        updateSystems.Clear();
        fixedUpdateSystems.Clear();
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

        foreach (var system in fixedUpdateSystems)
        {
            system.FixedUpdate(time);
        }

        World.Current?.IterateCallableComponents((entity, component) =>
        {
            try
            {
                component.FixedUpdate();
            }
            catch(Exception e)
            {
                Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling FixedUpdate: {e}");
            }
        });
    }

    public void Update()
    {
        if(Scene.current == null)
        {
            return;
        }

        var time = Time.deltaTime;

        foreach (var system in updateSystems)
        {
            system.Update(time);
        }

        World.Current?.IterateCallableComponents((entity, component) =>
        {
            if(component.STAPLE_JUST_ADDED)
            {
                component.STAPLE_JUST_ADDED = false;

                try
                {
                    component.Start();
                }
                catch (Exception e)
                {
                    Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling Start: {e}");
                }
            }

            try
            {
                component.Update();
            }
            catch (Exception e)
            {
                Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling Update: {e}");
            }
        });

        World.Current?.IterateCallableComponents((entity, component) =>
        {
            try
            {
                component.LateUpdate();
            }
            catch (Exception e)
            {
                Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling LateUpdate: {e}");
            }
        });
    }
}
