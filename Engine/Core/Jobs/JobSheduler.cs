using System;
using System.Threading.Tasks;

namespace Staple.Jobs;

/// <summary>
/// Scheduler for tasks/jobs
/// </summary>
public static class JobScheduler
{
    /// <summary>
    /// Amount of threads we can use
    /// </summary>
    private static readonly int Concurrency = System.Math.Max(1, Environment.ProcessorCount - 2);

    /// <summary>
    /// Gets the chunk size of an amount of iterations
    /// </summary>
    /// <param name="amount">The iteration amount</param>
    /// <returns>The chunk size based on the current concurrency</returns>
    public static int ChunkSize(int amount) => System.Math.Max(1, amount / Concurrency);

    /// <summary>
    /// Gets the chunk size for a specific iteration.
    /// Helps simplify for-style code
    /// </summary>
    /// <param name="i">The current iteration index</param>
    /// <param name="amount">The iteration amount</param>
    /// <returns>The chunk size for that specific index</returns>
    public static int ChunkSizeForIteration(int i, int amount)
    {
        var chunkSize = ChunkSize(amount);

        if(i + chunkSize > amount)
        {
            return amount - i;
        }

        return chunkSize;
    }

    /// <summary>
    /// Schedules a job to execute
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <returns>A job handle for the job</returns>
    public static JobHandle Schedule(IJob job)
    {
        var task = Task.Run(() =>
        {
            try
            {
                job.Execute();
            }
            catch(Exception e)
            {
                Log.Debug($"[JobScheduler] Failed to execute job {job.GetType().FullName}: {e}");
            }
        });

        return new JobHandle(task);
    }
}
