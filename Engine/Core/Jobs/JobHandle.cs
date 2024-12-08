using System;
using System.Threading.Tasks;

namespace Staple.Jobs;

/// <summary>
/// Contains a handle of a job execution
/// </summary>
public readonly struct JobHandle : IEquatable<JobHandle>
{
    internal readonly Task task;

    internal JobHandle(Task task)
    {
        this.task = task;
    }

    /// <summary>
    /// Checks whether the task was completed
    /// </summary>
    public bool Completed => task?.IsCompleted ?? true;

    /// <summary>
    /// Waits for the handle to complete
    /// </summary>
    public readonly void Complete()
    {
        if(task?.IsCompleted ?? true)
        {
            return;
        }

        task.Wait();
    }

    public override readonly bool Equals(object obj)
    {
        return obj is JobHandle handle && Equals(handle);
    }

    public readonly bool Equals(JobHandle other)
    {
        return task == other.task;
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(task);
    }

    public static bool operator ==(JobHandle left, JobHandle right)
    {
        return left.task == right.task;
    }

    public static bool operator !=(JobHandle left, JobHandle right)
    {
        return !(left == right);
    }
}
