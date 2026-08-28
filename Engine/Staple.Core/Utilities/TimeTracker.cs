using System.Diagnostics;

namespace Staple.Utilities;

/// <summary>
/// Time tracking class
/// </summary>
public class TimeTracker
{
    private float timeStamp;
    private float timeMultiplier = 1.0f / (float)Stopwatch.Frequency;

    /// <summary>
    /// The time since our last check. Resets the tracked time.
    /// </summary>
    public float ElapsedTime
    {
        get
        {
            if(timeStamp == 0)
            {
                timeStamp = Stopwatch.GetTimestamp() * timeMultiplier;
            }

            var current = Stopwatch.GetTimestamp() * timeMultiplier;

            var difference = current - timeStamp;

            timeStamp = current;

            return difference;
        }
    }

    /// <summary>
    /// The time since our last reset. Does not reset the tracked time.
    /// </summary>
    public float ElapsedTimeNoReset
    {
        get
        {
            if (timeStamp == 0)
            {
                timeStamp = Stopwatch.GetTimestamp() * timeMultiplier;
            }

            var current = Stopwatch.GetTimestamp() * timeMultiplier;

            var difference = current - timeStamp;

            return difference;
        }
    }

    /// <summary>
    /// Resets this timer.
    /// </summary>
    public void Reset()
    {
        timeStamp = Stopwatch.GetTimestamp() * timeMultiplier;
    }
}
