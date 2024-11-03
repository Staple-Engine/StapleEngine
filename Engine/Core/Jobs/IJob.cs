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
}
