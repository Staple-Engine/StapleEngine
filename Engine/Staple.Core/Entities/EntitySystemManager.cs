using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Staple;

/// <summary>
/// Manages entity systems
/// The player will automatically register entity systems.
/// </summary>
internal sealed class EntitySystemManager : ISubsystem
{
    internal static readonly byte Priority = 0;

    public SubsystemType type => SubsystemType.Update;

    private readonly HashSet<IEntitySystemUpdate> updateSystems = [];
    private readonly HashSet<IEntitySystemFixedUpdate> fixedUpdateSystems = [];
    private readonly HashSet<IEntitySystemLifecycle> lifecycleSystems = [];

    private readonly Dictionary<string, object> cachedSubclasses = [];

    private readonly Lock lockObject = new();

    internal Action onSubsystemsModified;

    public static readonly EntitySystemManager Instance = new();

    /// <summary>
    /// Finds all entity systems subclassing or implementing a specific type
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <returns>All entity systems currently loaded of that type</returns>
    public T[] FindEntitySystemsSubclassing<T>()
    {
        lock (lockObject)
        {
            if (cachedSubclasses.TryGetValue(typeof(T).FullName, out var subclasses))
            {
                return (T[])subclasses;
            }

            var outValue = new List<T>();

            foreach (var system in updateSystems)
            {
                if (system.GetType().IsSubclassOf(typeof(T)) ||
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
    }

    /// <summary>
    /// Unloads all entity subsystems that belong to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to unload from</param>
    internal void UnloadSystemsFromAssembly(Assembly assembly)
    {
        lock (lockObject)
        {
            var unloadedUpdate = new List<IEntitySystemUpdate>();
            var unloadedFixedUpdate = new List<IEntitySystemFixedUpdate>();
            var unloadedLifecycles = new List<IEntitySystemLifecycle>();

            foreach (var system in updateSystems)
            {
                if (system.GetType().Assembly == assembly)
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

            foreach(var system in lifecycleSystems)
            {
                if(system.GetType().Assembly == assembly)
                {
                    unloadedLifecycles.Add(system);
                }
            }

            foreach (var system in unloadedUpdate)
            {
                updateSystems.Remove(system);
            }

            foreach (var system in unloadedFixedUpdate)
            {
                fixedUpdateSystems.Remove(system);
            }

            foreach(var system in unloadedLifecycles)
            {
                lifecycleSystems.Remove(system);

                system.Shutdown();
            }

            cachedSubclasses.Clear();

            try
            {
                onSubsystemsModified?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[EntitySystemManager] Subsystem modified event exception: {e}");
            }
        }
    }

    /// <summary>
    /// Registers an entity system.
    /// </summary>
    /// <param name="system">The system to register</param>
    public void RegisterSystem(object system)
    {
        lock (lockObject)
        {
            if(fixedUpdateSystems.Any(x => x == system) ||
                updateSystems.Any(x => x == system) ||
                lifecycleSystems.Any(x => x == system))
            {
                return;
            }

            if (system is IWorldChangeReceiver receiver)
            {
                World.AddChangeReceiver(receiver);
            }

            if (system is IEntitySystemFixedUpdate fixedUpdate)
            {
                fixedUpdateSystems.Add(fixedUpdate);
            }

            if (system is IEntitySystemUpdate update)
            {
                updateSystems.Add(update);
            }

            if(system is IEntitySystemLifecycle lifecycle)
            {
                lifecycleSystems.Add(lifecycle);

                if(Platform.IsPlaying)
                {
                    StartupSystem(lifecycle);
                }
            }

            cachedSubclasses.Clear();

            try
            {
                onSubsystemsModified?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[EntitySystemManager] Subsystem modified event exception: {e}");
            }
        }
    }

    /// <summary>
    /// Starts up an entity system
    /// </summary>
    /// <param name="system">The entity system</param>
    public void StartupSystem(IEntitySystemLifecycle system)
    {
        if (system != null)
        {
            try
            {
                system.Startup();
            }
            catch (Exception e)
            {
                Log.Error($"[EntitySystemManager] Failed to startup {system.GetType().FullName}: {e}");

                lifecycleSystems.Remove(system);

                if (system is IEntitySystemFixedUpdate f)
                {
                    fixedUpdateSystems.Remove(f);
                }

                if (system is IEntitySystemUpdate u)
                {
                    updateSystems.Remove(u);
                }
            }
        }
    }

    /// <summary>
    /// Starts all systems
    /// </summary>
    public void StartupAllSystems()
    {
        lock(lockObject)
        {
            var lifecycles = lifecycleSystems.ToArray();

            foreach (var system in lifecycles)
            {
                StartupSystem(system);
            }
        }
    }

    /// <summary>
    /// Shuts down all systems
    /// </summary>
    public void ShutdownAllSystems()
    {
        lock (lockObject)
        {
            foreach (var system in lifecycleSystems)
            {
                try
                {
                    system.Shutdown();
                }
                catch (Exception e)
                {
                    Log.Error($"[EntitySystemManager] Failed to shutdown {system.GetType().FullName}: {e}");
                }
            }
        }
    }

    public void Shutdown()
    {
        lock (lockObject)
        {
            foreach(var system in lifecycleSystems)
            {
                try
                {
                    system.Shutdown();
                }
                catch(Exception e)
                {
                    Log.Error($"[EntitySystemManager] Failed to shutdown {system.GetType().FullName}: {e}");
                }
            }

            updateSystems.Clear();
            fixedUpdateSystems.Clear();
            lifecycleSystems.Clear();
        }
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

        lock(lockObject)
        {
            var time = Time.fixedDeltaTime;

            foreach (var system in fixedUpdateSystems)
            {
                using var profiler = new PerformanceProfiler(PerformanceProfilerType.Entity);

                system.FixedUpdate(time);
            }

            World.Current?.IterateCallableComponents((contents) =>
            {
                for(var i = 0; i < contents.Length; i++)
                {
                    var (entity, component) = contents[i];

                    try
                    {
                        component.FixedUpdate();
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling FixedUpdate: {e}");
                    }
                }
            });
        }
    }

    public void Update()
    {
        if(Scene.current == null)
        {
            return;
        }

        lock (lockObject)
        {
            var time = Time.deltaTime;

            foreach (var system in updateSystems)
            {
                using var profiler = new PerformanceProfiler(PerformanceProfilerType.Entity);

                system.Update(time);
            }

            World.Current?.IterateCallableComponents((contents) =>
            {
                for(var i = 0; i < contents.Length; i++)
                {
                    var (entity, component) = contents[i];

                    if (component.STAPLE_JUST_ADDED)
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
                }
            });

            World.Current?.IterateCallableComponents((contents) =>
            {
                for (var i = 0; i < contents.Length; i++)
                {
                    var (entity, component) = contents[i];

                    try
                    {
                        component.LateUpdate();
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"{entity.Name} ({component.GetType().FullName}): Exception thrown while handling LateUpdate: {e}");
                    }
                }
            });
        }
    }
}
