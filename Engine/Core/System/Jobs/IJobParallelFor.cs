namespace Staple.Jobs;

/// <summary>
/// Job interface for parallel iterations
/// </summary>
public interface IJobParallelFor
{
    /// <summary>
    /// How many threads to limit this to (use 0 to use as many as needed)
    /// </summary>
    int ThreadCount { get; }

    /// <summary>
    /// The amount of iterations per job
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// Execute the job at a specific index
    /// </summary>
    /// <param name="index">The index to iterate</param>
    void Execute(int index);

    /// <summary>
    /// Called after everything finishes
    /// </summary>
    void Finish();
}
