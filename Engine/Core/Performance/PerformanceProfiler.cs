using Staple.Internal;
using System;

namespace Staple;

public sealed class PerformanceProfiler : IDisposable
{
    private readonly PerformanceProfilerType type;
    private readonly DateTime startTime = DateTime.UtcNow;

    public PerformanceProfiler(PerformanceProfilerType type)
    {
        this.type = type;
    }

    public void Dispose()
    {
        PerformanceProfilerSystem.AddCounter(type, (int)(DateTime.UtcNow - startTime).TotalMilliseconds);
    }
}
