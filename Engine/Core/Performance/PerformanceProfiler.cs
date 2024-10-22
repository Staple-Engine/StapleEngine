using Staple.Internal;
using System;
using System.Runtime.CompilerServices;

namespace Staple;

public sealed class PerformanceProfiler(PerformanceProfilerType type) : IDisposable
{
    private readonly PerformanceProfilerType type = type;
    private readonly DateTime startTime = DateTime.UtcNow;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Report()
    {
        PerformanceProfilerSystem.AddCounter(type, (int)(DateTime.UtcNow - startTime).TotalMilliseconds);
    }

    public void Dispose()
    {
        Report();
    }
}
