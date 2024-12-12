using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal;

internal static class ObservableManager
{
    private static readonly List<WeakReference<IObservableBox>> boxes = [];

    public static void RemoveAll(Assembly assembly)
    {
        for(var i = boxes.Count - 1; i >= 0; i--)
        {
            var box = boxes[i];

            if(box.TryGetTarget(out var c))
            {
                c.RemoveAll(assembly);
            }
        }
    }

    public static void Add(IObservableBox box)
    {
        foreach(var b in boxes)
        {
            if(b.TryGetTarget(out var c) && c == box)
            {
                return;
            }
        }

        boxes.Add(new(box));
    }

    public static void Remove(IObservableBox box)
    {
        for(var i = boxes.Count - 1; i >= 0; i--)
        {
            var b = boxes[i];

            if(b.TryGetTarget(out var c) && c == box)
            {
                boxes.RemoveAt(i);
            }
        }
    }
}
