using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple;

[Serializable]
public class EntityCallbackEntry
{
    public int entityID;

    public string className;

    public string methodName;
}

[Serializable]
public class EntityCallback
{
    private class EntityCallbackEntryCache
    {
        public Entity entity;
        public Type type;
        public MethodInfo methodInfo;
    }

    [SerializeField]
    private List<EntityCallbackEntry> persistentCallbacks = new();

    private HashSet<Action> callbacks = new();
    private List<EntityCallbackEntryCache> persistentCache = new();

    public void AddListener(Action callback)
    {
        callbacks.Add(callback);
    }

    public void RemoveListener(Action callback)
    {
        callbacks.Remove(callback);
    }

    public void RemoveAllListeners()
    {
        callbacks.Clear();
    }

    internal void ClearCache()
    {
        persistentCache.Clear();
    }

    internal void AddPersistentCallback(EntityCallbackEntry entry)
    {
        persistentCallbacks.Add(entry);
    }

    internal void RemovePersistentCallback(EntityCallbackEntry entry)
    {
        persistentCallbacks.Remove(entry);
    }

    internal IEnumerator<EntityCallbackEntry> PersistentCallbacks()
    {
        return persistentCallbacks.GetEnumerator();
    }

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
