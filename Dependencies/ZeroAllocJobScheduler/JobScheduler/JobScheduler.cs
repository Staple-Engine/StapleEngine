using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: CLSCompliant(true)]

namespace Schedulers;

/// <summary>
///     A <see cref="JobScheduler"/> schedules and processes <see cref="IJob"/>s asynchronously. Better-suited for larger jobs due to its underlying events. 
/// </summary>
public partial class JobScheduler : IDisposable
{
    /// <summary>
    /// Contains configuration settings for <see cref="JobScheduler"/>.
    /// </summary>
    public struct Config
    {
        /// <summary>
        /// Create a new <see cref="Config"/> for a <see cref="JobScheduler"/> with all default settings.
        /// </summary>
        public Config() { }

        /// <summary>
        /// Defines the maximum expected number of concurrent jobs. Increasing this number will allow more jobs to be scheduled
        /// without spontaneous allocation, but will increase total memory consumption and decrease performance.
        /// If unset, the default is <c>32</c>
        /// </summary>
        public int MaxExpectedConcurrentJobs { get; set; } = 32;

        /// <summary>
        /// Whether to use Strict Allocation Mode for this <see cref="JobScheduler"/>. If an allocation might occur, the JobScheduler
        /// will throw a <see cref="MaximumConcurrentJobCountExceededException"/>.
        /// Not recommended for production environments (spontaneous allocation is probably usually better than crashing the program).
        /// </summary>
        public bool StrictAllocationMode { get; set; } = false;

        /// <summary>
        /// The process name to use for spawned child threads. By default, set to the current domain's <see cref="AppDomain.FriendlyName"/>.
        /// Thread will be named "prefix0" for the first thread, "prefix1" for the second thread, etc.
        /// </summary>
        public string ThreadPrefixName { get; set; } = AppDomain.CurrentDomain.FriendlyName;

        /// <summary>
        /// The amount of worker threads to use. By default, set to <see cref="Environment.ProcessorCount"/>, the amount of hardware processors 
        /// available on the system.
        /// </summary>
        public int ThreadCount { get; set; } = Environment.ProcessorCount;
    }

    /// <summary>
    /// Thrown when <see cref="Config.StrictAllocationMode"/> is enabled and the <see cref="JobScheduler"/> goes over its <see cref="Config.MaxExpectedConcurrentJobs"/>.
    /// </summary>
    public class MaximumConcurrentJobCountExceededException : Exception
    {
        internal MaximumConcurrentJobCountExceededException() : base($"{nameof(JobScheduler)} has gone over its {nameof(Config.MaxExpectedConcurrentJobs)} value! " +
            $"Increase that value or disable {nameof(Config.StrictAllocationMode)} to allow spontaneous allocations.")
        {
        }
    }

    // Tracks how many threads are alive; interlocked
    private int _threadsAlive = 0;

    // internally visible for testing
    internal int ThreadsAlive
    {
        get => _threadsAlive;
    }

    private readonly bool _strictAllocationMode;
    private readonly int _maxConcurrentJobs;

    /// <summary>
    /// The actual number of threads this <see cref="JobScheduler"/> was spawned with.
    /// </summary>
    public int ThreadCount { get; }

    // temporary list for passing deps into JobPool
    private readonly List<JobHandle> _dependencyCache;

    // a pool for recyling managed Jobs
    private readonly ConcurrentQueue<Job> _jobPool;

    private static int _lastInstanceId = -1;

    /// <summary>
    ///     A unique ID for this particular Scheduler
    /// </summary>
    internal int InstanceId { get; }

    private int _lastJobId = -1;

    /// <summary>
    /// Creates an instance of the <see cref="JobScheduler"/>
    /// </summary>
    /// <param name="settings">The <see cref="Config"/> to use for this instance of <see cref="JobScheduler"/></param>
    public JobScheduler(in Config settings)
    {
        InstanceId = Interlocked.Increment(ref _lastInstanceId);
        MainThreadID = Thread.CurrentThread.ManagedThreadId;

        ThreadCount = settings.ThreadCount;
        if (ThreadCount <= 0)
        {
            ThreadCount = Environment.ProcessorCount;
        }

        _strictAllocationMode = settings.StrictAllocationMode;
        _maxConcurrentJobs = settings.MaxExpectedConcurrentJobs;
        _dependencyCache = new(settings.MaxExpectedConcurrentJobs - 1);

        JobHandle.InitializeScheduler(InstanceId, this, _maxConcurrentJobs);

        // pre-fill all of our data structures up to the concurrent job max
        QueuedJobs = new(settings.MaxExpectedConcurrentJobs);

        // ConcurrentQueue doesn't have a segment size constructor so we have to use a hack with the IEnumerable constructor.
        // First, we add a bunch of dummy jobs to the old queue...
        for (var i = 0; i < settings.MaxExpectedConcurrentJobs; i++)
        {
            QueuedJobs.Add(null!); // this null is temporary
        }

        // ... then, we initialize the ConcurrentQueue with that collection. The segment size will be set to the count.
        // Note that this line WILL produce garbage due to IEnumerable iteration! (always boxes multiple enumerator structs)
        MasterQueue = new(QueuedJobs);
        // We also do the pool, just to have the segment size.
        _jobPool = new(QueuedJobs);

        // Then, we dequeue everything from the ConcurrentQueue. We can't Clear() because that'll nuke the segment.
        while (!MasterQueue.IsEmpty)
        {
            MasterQueue.TryDequeue(out var _);
        }

        while (!_jobPool.IsEmpty)
        {
            _jobPool.TryDequeue(out var _);
        }

        // And then normally clear the normal queue we used.
        QueuedJobs.Clear();

        // Now that our segment size is set, we fill the jobs pool with actual preallocated jobs ready for scheduling.
        for (var i = 0; i < settings.MaxExpectedConcurrentJobs; i++)
        {
            // this will automatically pool
            var j = new Job(settings.MaxExpectedConcurrentJobs - 1, ThreadCount, this, Interlocked.Increment(ref _lastJobId));
            JobHandle.TrackJob(InstanceId, j);
        }

        InitAlgorithm(ThreadCount, _maxConcurrentJobs, CancellationTokenSource.Token);

        // we count us as a thread
        _threadsAlive = ThreadCount;

        // spawn all the child threads
        for (var i = 0; i < ThreadCount; i++)
        {
            var c = i;
            var thread = new Thread(WorkerLoop)
            {
                Name = $"{settings.ThreadPrefixName}{i}"
            };
            thread.Start(i);
        }
    }

    /// <summary>
    /// Tracks which thread the JobScheduler was constructed on
    /// </summary>
    private int MainThreadID { get; }

    /// <summary>
    /// Tracks the overall state of all threads; when canceled in Dispose, all child threads are exited
    /// </summary>
    private CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    /// Jobs scheduled by the scheduler (NOT other jobs), but not yet flushed to the threads
    /// </summary>
    private List<Job> QueuedJobs { get; }

    /// <summary>
    /// Returns true if this is the main thread the scheduler was created on; false otherwise
    /// </summary>
    public bool IsMainThread
    {
        get => Thread.CurrentThread.ManagedThreadId == MainThreadID;
    }

    private Job GetPooledJob()
    {
        Job job;
        // This will never fail due to concurrency issues because the main thread is the only ones allowed to dequeue
        while (!_jobPool.TryDequeue(out job))
        {
            if (_strictAllocationMode)
            {
                throw new MaximumConcurrentJobCountExceededException();
            }
            // We are spontaneously allocating, so to save memory, don't use an initial size
            // This will automatically pool!
            var j = new Job(0, ThreadCount, this, Interlocked.Increment(ref _lastJobId));
            JobHandle.TrackJob(InstanceId, j);
        }

        return job;
    }

    /// <summary>
    ///     Schedules a <see cref="IJob"/> and returns its <see cref="JobHandle"/>.
    /// </summary>
    /// <param name="work">The <see cref="IJob"/>.</param>
    /// <param name="dependency">The <see cref="JobHandle"/>-Dependency.</param>
    /// <param name="dependencies">A list of additional <see cref="JobHandle"/>-Dependencies.</param>
    /// <param name="parallelWork">A parallel job, if we want to schedule one.</param>
    /// <param name="amount">The amount of times to run the parallel job.</param>
    /// <returns>A <see cref="JobHandle"/>.</returns>
    /// <exception cref="InvalidOperationException">If called on a different thread than the <see cref="JobScheduler"/> was constructed on</exception>
    /// <exception cref="MaximumConcurrentJobCountExceededException">If the maximum amount of concurrent jobs is at maximum, and strict mode is enabled.</exception>
    private JobHandle Schedule(IJob? work, JobHandle? dependency = null, ReadOnlySpan<JobHandle> dependencies = new(), IJobParallelFor? parallelWork = null, int amount = 0)
    {
        if (!IsMainThread)
        {
            throw new InvalidOperationException($"Can only call {nameof(Schedule)} from the thread that spawned the {nameof(JobScheduler)}!");
        }

        _dependencyCache.Clear();

        foreach (var d in dependencies)
        {
            _dependencyCache.Add(d);
        }

        if (dependency is not null)
        {
            _dependencyCache.Add(dependency.Value);
        }

        JobHandle? handle = null;

        // Schedule a regular job
        if (parallelWork is null)
        {
            var pooledJob = GetPooledJob();
            handle = pooledJob.Schedule(work, _dependencyCache, out var ready);

            // if we're ready, we can go ahead and prep the job. If not, we leave that up to the dependencies.
            if (ready)
            {
                QueuedJobs.Add(pooledJob);
            }
        }
        // Schedule a parallel job
        else
        {
            if (parallelWork.BatchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parallelWork.BatchSize));
            }

            var threads = parallelWork.ThreadCount <= 0 ? ThreadCount : Math.Min(parallelWork.ThreadCount, ThreadCount);
            var batches = (int)MathF.Ceiling(amount / (float)parallelWork.BatchSize);
            threads = Math.Min(threads, batches);

            for (var i = 0; i < threads; i++)
            {
                var pooledJob = GetPooledJob();

                bool ready;
                // we treat the first job as the master
                if (i == 0)
                {
                    handle = pooledJob.Schedule(work, _dependencyCache, out ready, parallelWork, null, amount, threads, i);
                }
                else
                {
                    // other jobs use the master to store their stuff
                    pooledJob.Schedule(work, _dependencyCache, out ready, parallelWork, handle, amount, threads, i);
                }

                // if we're ready, we can go ahead and prep the job. If not, we leave that up to the dependencies.
                if (ready)
                {
                    QueuedJobs.Add(pooledJob);
                }
            }
        }

        Debug.Assert(handle is not null);
        return handle.Value;
    }

    /// <summary>
    ///     Schedules an <see cref="IJob"/>. It is only queued up, and will only begin processing when the user calls <see cref="Flush()"/> or when any in-progress dependencies complete.
    /// </summary>
    /// <param name="job">The job to process</param>
    /// <param name="dependency">A job that must complete before this job can be run</param>
    /// <returns>Its <see cref="JobHandle"/>.</returns>
    /// <exception cref="InvalidOperationException">If called on a different thread than the <see cref="JobScheduler"/> was constructed on</exception>
    /// <exception cref="MaximumConcurrentJobCountExceededException">If the maximum amount of concurrent jobs is at maximum, and strict mode is enabled.</exception>
    public JobHandle Schedule(IJob job, JobHandle? dependency = null)
    {
        if (dependency is not null)
        {
            CheckForSchedulerEquality(dependency.Value);
        }

        return Schedule(job, dependency, null);
    }

    /// <summary>
    ///     Schedules an <see cref="IJobParallelFor"/>. It is only queued up, and will only begin processing when the user calls
    /// <see cref="Flush()"/> or when any in-progress dependencies complete.
    /// </summary>
    /// <remarks>
    ///     Note that this will schedule as many jobs as specified in <see cref="IJobParallelFor"/> or the maximum thread count, whichever is less
    ///     (or the maximum thread count if the threads provided are 0). See <see cref="IJobParallelFor"/> for details.
    /// </remarks>
    /// <param name="job">The <see cref="IJobParallelFor"/> to schedule.</param>
    /// <param name="amount">
    ///     The amount of indices to execute.
    ///     <see cref="IJobParallelFor.Execute(int)"/> will be called for each value in <c>[0, <paramref name="amount"/>)</c>.
    /// </param>
    /// <param name="dependency">A <see cref="JobHandle"/> dependency to require completion of first.</param>
    /// <returns>The <see cref="JobHandle"/> of a job representing the full task.</returns>
    /// <exception cref="InvalidOperationException">If called on a different thread than the <see cref="JobScheduler"/> was constructed on</exception>
    /// <exception cref="MaximumConcurrentJobCountExceededException">If the maximum amount of concurrent jobs is at maximum, and strict mode is enabled.</exception>
    public JobHandle Schedule(IJobParallelFor job, int amount, JobHandle? dependency = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (dependency is not null)
        {
            CheckForSchedulerEquality(dependency.Value);
        }

        return Schedule(null, dependency, null, job, amount);
    }

    /// <summary>
    ///     Combine multiple dependencies into a single <see cref="JobHandle"/> which is scheduled.
    /// </summary>
    /// <param name="dependencies">A list of handles to depend on. Assumed to not contain duplicates.</param>
    /// <returns>The combined <see cref="JobHandle"/></returns>
    /// <exception cref="InvalidOperationException">If called on a different thread than the <see cref="JobScheduler"/> was constructed on</exception>
    /// <exception cref="MaximumConcurrentJobCountExceededException">If the maximum amount of concurrent jobs is at maximum, and strict mode is enabled.</exception>
    public JobHandle CombineDependencies(ReadOnlySpan<JobHandle> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            CheckForSchedulerEquality(dependency);
        }

        return Schedule(null, null, dependencies);
    }

    /// <summary>
    ///     Checks if the passed <see cref="JobHandle"/> equals this <see cref="JobScheduler"/>.
    /// </summary>
    /// <param name="dependency">The <see cref="JobHandle"/>.</param>
    /// <exception cref="InvalidOperationException">Is thrown when the passed handle has a different scheduler.</exception>
    private void CheckForSchedulerEquality(JobHandle dependency)
    {
        if (!ReferenceEquals(dependency.Scheduler, this))
        {
            throw new InvalidOperationException($"Job dependency was scheduled with a different {nameof(JobScheduler)}!");
        }
    }

    /// <summary>
    /// Flushes all queued <see cref="IJob"/>'s to the worker threads. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush()
    {
        if (!IsMainThread)
        {
            throw new InvalidOperationException($"Can only call {nameof(Flush)} from the thread that spawned the {nameof(JobScheduler)}!");
        }

        if (QueuedJobs.Count == 0)
        {
            return;
        }

        // QueuedJobs is guaranteed to only contain jobs that are ready
        foreach (var job in QueuedJobs)
        {
            // because this is a concurrentqueue (linkedlist implementation under the hood)
            // we don't have to worry about ordering issues (if someone dequeues while we do this, it'll just take the first one we added, which is fine)
            MasterQueue.Enqueue(job);
        }

        // clear the the incoming queue
        QueuedJobs.Clear();

        // algorithm 1
        _notifier.NotifyOne();
    }

    /// <summary>
    /// Blocks the thread until the given job ID has been completed. Can be called from Jobs.
    /// </summary>
    /// <param name="handle"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Complete(in JobHandle handle)
    {
        var job = handle.Job;
        if (job.TrySubscribe(handle.Version, out var waitHandle))
        {
            waitHandle.WaitOne();
            job.Unsubscribe(handle.Version);
        }
    }

    /// <summary>
    /// Called exclusively from <see cref="Job"/> when it wants to pool itself.
    /// </summary>
    /// <param name="job"></param>
    internal void PoolJob(Job job)
    {
        _jobPool.Enqueue(job);
    }

    /// <summary>
    /// Disposes all internals and notifies all threads to cancel.
    /// </summary>
    public void Dispose()
    {
        if (!IsMainThread)
        {
            throw new InvalidOperationException($"Can only call {nameof(Dispose)} from the thread that spawned the {nameof(JobScheduler)}!");
        }

        // notify all threads to cancel
        CancellationTokenSource.Cancel(false);

        QueuedJobs.Clear();
        MasterQueue.Clear();

        // notifier handles issues where threads might dispose this early for us
        // either this works and threads are signalled to go into shutdown mode, which eventually dispose..
        // or they somehow managed to 100% shutdown and dispose already, in which case his does nothing.
        _notifier.NotifyAll();

        JobHandle.DisposeScheduler(InstanceId);
    }
}
