using Staple;
using System;
using System.Threading;

namespace Baker;

internal static class WorkScheduler
{
    private static int taskCount = 0;
    private static int workCount = 0;
    private static object syncObject = new();

    public static void WaitForTasks()
    {
        for (; ; )
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

    public static void Dispatch(string fileName, Action task)
    {
        lock(syncObject)
        {
            taskCount++;
            workCount++;
        }

        ThreadPool.QueueUserWorkItem((_) =>
        {
            try
            {
                task();
            }
            catch(Exception e)
            {
                Log.Error($"Failed to process {fileName}: {e}");
            }

            lock (syncObject)
            {
                taskCount--;

                Console.WriteLine($"[{workCount - taskCount}/{workCount}] {fileName}");
            }
        });
    }
}
