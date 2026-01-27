using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

internal static class PerformanceProfilerSystem
{
    private static readonly Dictionary<PerformanceProfilerType, int> counters = [];

    private static readonly Dictionary<PerformanceProfilerType, int> frameCounters = [];

    private static readonly Dictionary<PerformanceProfilerType, int> combinedFrameCounters = [];

    private static readonly Dictionary<PerformanceProfilerType, int> averageFrameCounters = [];

    private static readonly Lock lockObject = new();

    private static DateTime lastAverageTime = DateTime.UtcNow;

    public static Dictionary<PerformanceProfilerType, int> FrameCounters
    {
        get
        {
            if (AppSettings.Current?.profilingMode != AppProfilingMode.Profiler)
            {
                return [];
            }

            lock(lockObject)
            {
                return new(frameCounters);
            }
        }
    }

    public static Dictionary<PerformanceProfilerType, int> AverageFrameCounters
    {
        get
        {
            if (AppSettings.Current?.profilingMode != AppProfilingMode.Profiler)
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
        if (AppSettings.Current?.profilingMode != AppProfilingMode.Profiler)
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
        if (AppSettings.Current?.profilingMode != AppProfilingMode.Profiler)
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

    public static void AddCounter(PerformanceProfilerType type, int ms)
    {
        if (AppSettings.Current?.profilingMode != AppProfilingMode.Profiler)
        {
            return;
        }

        lock(lockObject)
        {
            if (counters.TryGetValue(type, out var t))
            {
                t += ms;

                counters[type] = t;
            }
            else
            {
                counters.Add(type, ms);
            }
        }
    }
}
