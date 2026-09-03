using System.Diagnostics;

namespace Staple.Utilities;

/// <summary>
/// Time tracking class
/// </summary>
public class TimeTracker
{
    private long timeStamp;

    /// <summary>
    /// The time since our last check. Resets the tracked time.
    /// </summary>
    public float ElapsedTime
    {
        get
        {
            if(timeStamp == 0)
            {
                timeStamp = Stopwatch.GetTimestamp();
            }

            var elapsed = Stopwatch.GetElapsedTime(timeStamp);

            timeStamp = Stopwatch.GetTimestamp();

            return (float)elapsed.TotalSeconds;
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
                timeStamp = Stopwatch.GetTimestamp();
            }

            var elapsed = Stopwatch.GetElapsedTime(timeStamp);

            return (float)elapsed.TotalSeconds;
        }
    }

    /// <summary>
    /// Resets this timer.
    /// </summary>
    public void Reset()
    {
        timeStamp = Stopwatch.GetTimestamp();
    }
}
