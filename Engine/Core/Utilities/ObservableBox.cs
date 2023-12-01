using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal;

internal class ObservableBox
{
    class ObserverInfo
    {
        public Assembly assembly;
        public WeakReference<object> observer;
    }

    private List<ObserverInfo> observers = new();
    private List<object> stagingObservers = new();
    private object observerLock = new();

    public ObservableBox()
    {
        ObservableManager.Add(this);
    }

    public void RemoveAll(Assembly assembly)
    {
        lock(observerLock)
        {
            for(var i = observers.Count - 1; i >= 0; i--)
            {
                if (observers[i].assembly == assembly)
                {
                    observers.RemoveAt(i);
                }
            }
        }
    }

    public void AddObserver(object type)
    {
        lock(observerLock)
        {
            foreach(var item in observers)
            {
                if(item.observer.TryGetTarget(out var o) && o == type)
                {
                    return;
                }
            }

            observers.Add(new ObserverInfo()
            {
                assembly = type.GetType().Assembly,
                observer = new WeakReference<object>(type),
            });
        }
    }

    public void RemoveObserver(object type)
    {
        lock (observerLock)
        {
            foreach (var item in observers)
            {
                if (item.observer.TryGetTarget(out var o) && o == type)
                {
                    observers.Remove(item);

                    return;
                }
            }
        }
    }

    public void Emit(Action<object> callback)
    {
        lock(observerLock)
        {
            for(var i = observers.Count - 1; i >= 0; i--)
            {
                var item = observers[i];

                if (item.observer.TryGetTarget(out var o))
                {
                    stagingObservers.Add(o);
                }
                else
                {
                    observers.RemoveAt(i);
                }
            }

            foreach (var observer in stagingObservers)
            {
                callback(observer);
            }

            stagingObservers.Clear();
        }
    }
}
