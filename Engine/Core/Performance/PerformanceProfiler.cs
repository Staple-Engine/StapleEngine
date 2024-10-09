using Staple.Internal;
using System;

namespace Staple;

public sealed class PerformanceProfiler : IDisposable
{
    private readonly string name;
    private readonly DateTime startTime = DateTime.UtcNow;

    public PerformanceProfiler(string name)
    {
        this.name = name;
    }

    public void Dispose()
    {
        PerformanceProfilerSystem.AddCounter(name, (int)(DateTime.UtcNow - startTime).TotalMilliseconds);
    }
}
