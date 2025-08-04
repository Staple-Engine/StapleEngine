using System;

namespace Staple.Jobs;

/// <summary>
/// Job interface
/// </summary>
public interface IJob
{
    /// <summary>
    /// Called to execute the job
    /// </summary>
    void Execute();

    /// <summary>
    /// Called when there's a failure with a job due to an exception
    /// </summary>
    /// <param name="e">The exception</param>
    void Failure(Exception e);
}
