using System;

namespace Staple.Jobs;

/// <summary>
/// Scheduler for tasks/jobs
/// </summary>
public static class JobScheduler
{
    private class JobProxy : Schedulers.IJob
    {
        public IJob job;

        public void Execute() => job.Execute();
    }

    private class JobParallelForProxy : Schedulers.IJobParallelFor
    {
        public IJobParallelFor job;

        public int ThreadCount => job.ThreadCount;

        public int BatchSize => job.BatchSize;

        public void Execute(int index) => job.Execute(index);

        public void Finish() => job.Finish();
    }

    /// <summary>
    /// Amount of threads we can use
    /// </summary>
    private static readonly int Concurrency = System.Math.Max(1, Environment.ProcessorCount - 2);

    /// <summary>
    /// Internal job scheduler
    /// </summary>
    private static readonly Schedulers.JobScheduler jobScheduler = new(new()
    {
        ThreadPrefixName = "Staple Job Scheduler ",
        ThreadCount = 0,
        MaxExpectedConcurrentJobs = 64,
        StrictAllocationMode = false,
    });

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
        var handle = new JobHandle()
        {
            handle = jobScheduler.Schedule(new JobProxy()
            {
                job = job,
            }),
        };

        jobScheduler.Flush();

        return handle;
    }

    /// <summary>
    /// Schedules a parallel for job to execute
    /// </summary>
    /// <param name="job">The job to execute</param>
    /// <returns>A job handle for the job</returns>
    public static JobHandle Schedule(IJobParallelFor job, int count)
    {
        var handle = new JobHandle()
        {
            handle = jobScheduler.Schedule(new JobParallelForProxy()
            {
                job = job,
            }, count),
        };

        jobScheduler.Flush();

        return handle;
    }

    internal static void Dispose()
    {
        jobScheduler.Dispose();
    }
}
