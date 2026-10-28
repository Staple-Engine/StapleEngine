using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// A container of observers. Retains a weak reference to the observer.
/// </summary>
internal abstract class ObservableBox : IObservableBox
{
    class ObserverInfo
    {
        public Assembly assembly;
        public WeakReference<object> observer;
    }

    private readonly List<ObserverInfo> observers = [];
    private readonly ExpandableContainer<object> stagingObservers = new();

    /// <summary>
    /// Gets all the valid observer objects.
    /// </summary>
    /// <remarks>Calling it multiple times causes any other span from this property to become invalid since memory is reallocated</remarks>
    public Span<object> Observers
    {
        get
        {
            stagingObservers.Clear();

            for (var i = observers.Count - 1; i >= 0; i--)
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

            return stagingObservers.Contents;
        }
    }

    public ObservableBox()
    {
        ObservableManager.Add(this);
    }

    /// <summary>
    /// Removes all observers of a specific assembly. Useful for reloads.
    /// </summary>
    /// <param name="assembly">The assembly to remove</param>
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

    /// <summary>
    /// Adds a new observer
    /// </summary>
    /// <param name="observer">The new observer</param>
    public void AddObserver(object observer)
    {
        foreach(var item in observers)
        {
            if(item.observer.TryGetTarget(out var o) && o == observer)
            {
                return;
            }
        }

        observers.Add(new ObserverInfo()
        {
            assembly = observer.GetType().Assembly,
            observer = new WeakReference<object>(observer),
        });
    }

    /// <summary>
    /// Removes an observer
    /// </summary>
    /// <param name="observer">The observer to remove</param>
    public void RemoveObserver(object observer)
    {
        foreach (var item in observers)
        {
            if (item.observer.TryGetTarget(out var o) && o == observer)
            {
                observers.Remove(item);

                return;
            }
        }
    }

    /// <summary>
    /// An action to perform when <see cref="Emit"/> is called
    /// </summary>
    /// <param name="observer">The observer being processed</param>
    protected abstract void EmitAction(object observer);

    /// <summary>
    /// Emits <see cref="EmitAction(object)"/> to each observer
    /// </summary>
    public void Emit()
    {
        foreach (var observer in Observers)
        {
            EmitAction(observer);
        }
    }
}
