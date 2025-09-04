using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Staple.Internal;

internal abstract class ObservableBoxStrong : IObservableBox
{
    class ObserverInfo
    {
        public Assembly assembly;
        public object observer;
    }

    private readonly List<ObserverInfo> observers = [];
    private readonly List<object> stagingObservers = [];
    private readonly Lock observerLock = new();

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

    public abstract void EmitAction(object observer);

    public void Emit()
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
                EmitAction(observer);
            }

            stagingObservers.Clear();
        }
    }
}
