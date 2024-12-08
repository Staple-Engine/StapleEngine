using Staple.Jobs;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Executes a background task
    /// </summary>
    /// <param name="handle">The job handle</param>
    public void StartBackgroundTask(JobHandle handle)
    {
        lock(backgroundLock)
        {
            backgroundHandles.Add(handle);

            showingProgress = true;
            progressFraction = 0;
        }
    }

    /// <summary>
    /// Sets the current background progress and message
    /// </summary>
    /// <param name="progress">The progress percentage (0-1)</param>
    /// <param name="message">The message</param>
    public void SetBackgroundProgress(float progress, string message)
    {
        ThreadHelper.Dispatch(() =>
        {
            progressFraction = progress;
            progressMessage = message;
        });
    }
}
