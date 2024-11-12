using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal;

internal static class ObservableManager
{
    private static readonly List<WeakReference<ObservableBox>> boxes = [];
    private static readonly List<WeakReference<ObservableBoxStrong>> strongBoxes = [];
    private static readonly object lockObject = new();

    public static void RemoveAll(Assembly assembly)
    {
        lock (lockObject)
        {
            for(var i = boxes.Count - 1; i >= 0; i--)
            {
                var box = boxes[i];

                if(box.TryGetTarget(out var c))
                {
                    c.RemoveAll(assembly);
                }
            }

            for (var i = strongBoxes.Count - 1; i >= 0; i--)
            {
                var box = strongBoxes[i];

                if (box.TryGetTarget(out var c))
                {
                    c.RemoveAll(assembly);
                }
            }
        }
    }

    public static void Add(ObservableBox box)
    {
        lock(lockObject)
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
    }

    public static void Add(ObservableBoxStrong box)
    {
        lock (lockObject)
        {
            foreach (var b in strongBoxes)
            {
                if (b.TryGetTarget(out var c) && c == box)
                {
                    return;
                }
            }

            strongBoxes.Add(new(box));
        }
    }

    public static void Remove(ObservableBox box)
    {
        lock (lockObject)
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

    public static void Remove(ObservableBoxStrong box)
    {
        lock (lockObject)
        {
            for (var i = strongBoxes.Count - 1; i >= 0; i--)
            {
                var b = strongBoxes[i];

                if (b.TryGetTarget(out var c) && c == box)
                {
                    boxes.RemoveAt(i);
                }
            }
        }
    }
}
