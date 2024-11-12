
using Staple.Internal;
using System.Collections.Generic;
using System.Reflection;
using System;

internal class ObservableBoxStrong
{
    class ObserverInfo
    {
        public Assembly assembly;
        public object observer;
    }

    private List<ObserverInfo> observers = [];
    private List<object> stagingObservers = [];
    private object observerLock = new();

    public ObservableBoxStrong()
    {
        ObservableManager.Add(this);
    }

    public void RemoveAll(Assembly assembly)
    {
        lock (observerLock)
        {
            for (var i = observers.Count - 1; i >= 0; i--)
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
        lock (observerLock)
        {
            foreach (var item in observers)
            {
                if (item.observer == type)
                {
                    return;
                }
            }

            observers.Add(new ObserverInfo()
            {
                assembly = type.GetType().Assembly,
                observer = type,
            });
        }
    }

    public void RemoveObserver(object type)
    {
        lock (observerLock)
        {
            foreach (var item in observers)
            {
                if (item.observer == type)
                {
                    observers.Remove(item);

                    return;
                }
            }
        }
    }

    public void Emit(Action<object> callback)
    {
        lock (observerLock)
        {
            for (var i = observers.Count - 1; i >= 0; i--)
            {
                var item = observers[i];

                if (item.observer != null)
                {
                    stagingObservers.Add(item.observer);
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
