using System;
using System.Collections.Generic;

namespace Staple.Jobs;

/// <summary>
/// Contains a handle of a job execution
/// </summary>
public struct JobHandle : IEquatable<JobHandle>
{
    internal Schedulers.JobHandle handle;

    /// <summary>
    /// Waits for the handle to complete
    /// </summary>
    public readonly void Complete() => handle.Complete();

    /// <summary>
    /// Waits for a collection of handles to complete
    /// </summary>
    /// <param name="jobs">The list of jobs</param>
    public static void CompleteAll(ReadOnlySpan<JobHandle> jobs)
    {
        var allHandles = new Schedulers.JobHandle[jobs.Length];

        for (var i = 0; i < jobs.Length; i++)
        {
            allHandles[i] = jobs[i].handle;
        }

        Schedulers.JobHandle.CompleteAll(allHandles.AsSpan());
    }

    /// <summary>
    /// Waits for a collection of handles to complete
    /// </summary>
    /// <param name="jobs">The list of jobs</param>
    public static void CompleteAll(IReadOnlyList<JobHandle> jobs)
    {
        var allHandles = new Schedulers.JobHandle[jobs.Count];

        for (var i = 0; i < jobs.Count; i++)
        {
            allHandles[i] = jobs[i].handle;
        }

        Schedulers.JobHandle.CompleteAll(allHandles.AsSpan());
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is JobHandle handle && Equals(handle);
    }

    public readonly bool Equals(JobHandle other)
    {
        return handle == other.handle;
    }

    public readonly override int GetHashCode()
    {
        return handle.GetHashCode();
    }

    public static bool operator ==(JobHandle left, JobHandle right)
    {
        return left.handle.Equals(right.handle);
    }

    public static bool operator !=(JobHandle left, JobHandle right)
    {
        return !(left == right);
    }
}
