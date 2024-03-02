using System;
using System.Threading;

namespace Baker;

internal static class WorkScheduler
{
    private static int taskCount = 0;
    private static object syncObject = new();

    public static void WaitForTasks()
    {
        for(; ; )
        {
            lock(syncObject)
            {
                if(taskCount <= 0)
                {
                    break;
                }
            }
        }
    }

    public static void Dispatch(Action task)
    {
        lock(syncObject)
        {
            taskCount++;
        }

        ThreadPool.QueueUserWorkItem((_) =>
        {
            try
            {
                task();
            }
            catch(Exception)
            {
            }

            lock (syncObject)
            {
                taskCount--;
            }
        });
    }
}
