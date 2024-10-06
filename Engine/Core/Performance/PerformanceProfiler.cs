using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal static class PerformanceProfiler
{
    public delegate void OnFinishFrameCallback(Dictionary<string, float> counters);

    private static Dictionary<string, float> counters = [];

    private static object lockObject = new();

    public static event OnFinishFrameCallback OnFinishFrame;

    public static void Measure(string name, Action action)
    {
        var startTime = DateTime.Now;

        try
        {
            action?.Invoke();
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());

            return;
        }

        var elapsedTime = DateTime.Now - startTime;

        AddCounter(name, (float)elapsedTime.TotalSeconds);
    }

    public static void StartFrame()
    {
        lock (lockObject)
        {
            counters.Clear();
        }
    }

    public static void FinishFrame()
    {
        lock (lockObject)
        {
            OnFinishFrame?.Invoke(counters);
        }
    }

    public static void AddCounter(string name, float time)
    {
        lock (lockObject)
        {
            if (counters.TryGetValue(name, out var t))
            {
                t += time;

                counters[name] = t;
            }
            else
            {
                counters.Add(name, time);
            }
        }
    }
}
