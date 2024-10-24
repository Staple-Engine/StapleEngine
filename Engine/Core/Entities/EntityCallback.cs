using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple;

/// <summary>
/// Contains a way to add callbacks to actions and an entity, a component, and its methods
/// </summary>
[Serializable]
public sealed class EntityCallback
{
    [Serializable]
    internal class EntityCallbackEntry
    {
        public int entityID;

        public string className;

        public string methodName;
    }

    private class EntityCallbackEntryCache
    {
        public Entity entity;
        public Type type;
        public MethodInfo methodInfo;
    }

    [SerializeField]
    private readonly List<EntityCallbackEntry> persistentCallbacks = [];

    private readonly HashSet<Action> callbacks = [];
    private readonly List<EntityCallbackEntryCache> persistentCache = [];

    /// <summary>
    /// Adds a callback to this entity callback
    /// </summary>
    /// <param name="callback">The callback</param>
    public void AddListener(Action callback)
    {
        callbacks.Add(callback);
    }

    /// <summary>
    /// Removes a callback from this entity callback
    /// </summary>
    /// <param name="callback">The callback</param>
    public void RemoveListener(Action callback)
    {
        callbacks.Remove(callback);
    }

    /// <summary>
    /// Removes all callbacks from this entity callback
    /// </summary>
    public void RemoveAllListeners()
    {
        callbacks.Clear();
    }

    /// <summary>
    /// Clears the persistent cache of this entity callback
    /// </summary>
    internal void ClearCache()
    {
        persistentCache.Clear();
    }

    /// <summary>
    /// Adds a persistent callback
    /// </summary>
    /// <param name="entry">The new entry</param>
    internal void AddPersistentCallback(EntityCallbackEntry entry)
    {
        persistentCallbacks.Add(entry);
    }

    /// <summary>
    /// Removes a persistent callback
    /// </summary>
    /// <param name="entry">The entry</param>
    internal void RemovePersistentCallback(EntityCallbackEntry entry)
    {
        persistentCallbacks.Remove(entry);
    }

    internal IEnumerable<EntityCallbackEntry> PersistentCallbacks()
    {
        return persistentCallbacks;
    }

    /// <summary>
    /// Updates the persistent cache of this entity callback
    /// </summary>
    internal void UpdateCache()
    {
        if (persistentCache.Count == 0)
        {
            foreach (var callback in persistentCallbacks)
            {
                try
                {
                    var entity = Scene.FindEntity(callback.entityID);

                    if (entity.IsValid == false)
                    {
                        persistentCache.Add(null);

                        continue;
                    }

                    var type = TypeCache.GetType(callback.className);

                    if (type == null || entity.TryGetComponent(out _, type) == false)
                    {
                        persistentCache.Add(null);

                        continue;
                    }

                    var method = type.GetMethod(callback.methodName, BindingFlags.Public | BindingFlags.Instance);

                    if (method == null)
                    {
                        persistentCache.Add(null);

                        continue;
                    }
                }
                catch (Exception e)
                {
                    persistentCache.Add(null);

                    continue;
                }
            }
        }
    }

    /// <summary>
    /// Invokes the callbacks in this entity callback
    /// </summary>
    public void Invoke()
    {
        UpdateCache();

        foreach(var callback in persistentCache)
        {
            if(callback == null ||
                callback.entity.TryGetComponent(out var component, callback.type) == false)
            {
                continue;
            }

            try
            {
                callback.methodInfo.Invoke(component, null);
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        foreach(var callback in callbacks)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}
