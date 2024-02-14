using System;
using System.Threading;

namespace Staple.Editor;

internal partial class StapleEditor
{
    public void StartBackgroundTask(BackgroundTaskProgressCallback callback)
    {
        Thread thread = null;
        
        thread = new Thread(() =>
        {
            for (; ; )
            {
                var shouldQuit = false;
                float t = 0;

                lock(backgroundLock)
                {
                    if(shouldTerminate)
                    {
                        return;
                    }

                    t = progressFraction;
                }

                try
                {

                    shouldQuit = callback(ref t);
                }
                catch(Exception e)
                {
                    Log.Error($"Background Task Error: {e}");

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
