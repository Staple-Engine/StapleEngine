﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal;

internal abstract class ObservableBox : IObservableBox
{
    class ObserverInfo
    {
        public Assembly assembly;
        public WeakReference<object> observer;
    }

    private readonly List<ObserverInfo> observers = [];
    private readonly List<object> stagingObservers = [];

    public ObservableBox()
    {
        ObservableManager.Add(this);
    }

    public void RemoveAll(Assembly assembly)
    {
        for(var i = observers.Count - 1; i >= 0; i--)
        {
            if (observers[i].assembly == assembly)
            {
                observers.RemoveAt(i);
            }
        }
    }

    public void AddObserver(object type)
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

    public void RemoveObserver(object type)
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

    public abstract void EmitAction(object observer);

    public void Emit()
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
            EmitAction(observer);
        }

        stagingObservers.Clear();
    }
}
