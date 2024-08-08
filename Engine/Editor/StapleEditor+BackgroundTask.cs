using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Executes a background task
    /// </summary>
    /// <param name="callback">The background callback</param>
    public void StartBackgroundTask(IEnumerator<(bool, string, float)> callback)
    {
        Thread thread = null;

        showingProgress = true;
        progressFraction = 0;

        thread = new Thread(() =>
        {
            for (; ; )
            {
                var shouldQuit = false;
                float t = 0;
                string s = "";

                lock(backgroundLock)
                {
                    if(shouldTerminate)
                    {
                        return;
                    }

                    t = progressFraction;
                    s = progressMessage;
                }

                try
                {
                    shouldQuit = callback.Current.Item1;

                    s = callback.Current.Item2;
                    
                    t = callback.Current.Item3;
                }
                catch(Exception e)
                {
                    Log.Error($"Background Task Error: {e}");

                    shouldQuit = true;
                }

                if (callback.MoveNext() == false)
                {
                    shouldQuit = true;
                }

                if(shouldQuit)
                {
                    lock(backgroundLock)
                    {
                        backgroundThreads.Remove(thread);
                    }

                    return;
                }

                lock(backgroundLock)
                {
                    progressFraction = t;
                    progressMessage = s;
                }
            }
        });

        lock(backgroundLock)
        {
            backgroundThreads.Add(thread);
        }

        thread.Start();
    }
}
