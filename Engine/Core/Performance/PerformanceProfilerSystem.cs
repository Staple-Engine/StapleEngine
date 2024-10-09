using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal static class PerformanceProfilerSystem
{
    private static readonly Dictionary<string, int> counters = [];

    private static readonly Dictionary<string, int> frameCounters = [];

    private static readonly Dictionary<string, int> combinedFrameCounters = [];

    private static readonly Dictionary<string, int> averageFrameCounters = [];

    private static readonly object lockObject = new();

    private static DateTime lastAverageTime = DateTime.UtcNow;

    public static Dictionary<string, int> FrameCounters
    {
        get
        {
            if (AppSettings.Current?.profilingMode != AppSettings.ProfilingMode.PerformanceOverlay)
            {
                return [];
            }

            lock(lockObject)
            {
                return new(frameCounters);
            }
        }
    }

    public static Dictionary<string, int> AverageFrameCounters
    {
        get
        {
            if (AppSettings.Current?.profilingMode != AppSettings.ProfilingMode.PerformanceOverlay)
            {
                return [];
            }

            lock (lockObject)
            {
                return new(averageFrameCounters);
            }
        }
    }

    public static void StartFrame()
    {
        if (AppSettings.Current?.profilingMode != AppSettings.ProfilingMode.PerformanceOverlay)
        {
            return;
        }

        lock(lockObject)
        {
            counters.Clear();
        }
    }

    public static void FinishFrame()
    {
        if (AppSettings.Current?.profilingMode != AppSettings.ProfilingMode.PerformanceOverlay)
        {
            return;
        }

        lock(lockObject)
        {
            frameCounters.Clear();

            foreach (var pair in counters)
            {
                if(combinedFrameCounters.TryGetValue(pair.Key, out var value))
                {
                    value += pair.Value;

                    combinedFrameCounters[pair.Key] = value;
                }
                else
                {
                    combinedFrameCounters.Add(pair.Key, pair.Value);
                }

                frameCounters.AddOrSetKey(pair.Key, pair.Value);
            }

            counters.Clear();

            if ((DateTime.UtcNow - lastAverageTime).TotalSeconds >= 1)
            {
                lastAverageTime = DateTime.UtcNow;

                averageFrameCounters.Clear();

                foreach(var pair in combinedFrameCounters)
                {
                    averageFrameCounters.Add(pair.Key, pair.Value / (Time.FPS == 0 ? 1 : Time.FPS));
                }

                combinedFrameCounters.Clear();
            }
        }
    }

    public static void AddCounter(string name, int ms)
    {
        if (AppSettings.Current?.profilingMode != AppSettings.ProfilingMode.PerformanceOverlay)
        {
            return;
        }

        lock(lockObject)
        {
            if (counters.TryGetValue(name, out var t))
            {
                t += ms;

                counters[name] = t;
            }
            else
            {
                counters.Add(name, ms);
            }
        }
    }
}
