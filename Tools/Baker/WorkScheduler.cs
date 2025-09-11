using Staple;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baker;

internal class WorkScheduler
{
    private int taskCount = 0;
    private int workCount = 0;

    public bool logCompletion = true;

    private readonly Lock syncObject = new();

    public static readonly WorkScheduler Main = new();

    public void WaitForTasks()
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

            Thread.Sleep(25);
        }
    }

    public void Dispatch(string fileName, Action task)
    {
        lock(syncObject)
        {
            taskCount++;
            workCount++;
        }

        Task.Run(() =>
        {
            try
            {
                task();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to process {fileName}: {e}");
            }

            lock (syncObject)
            {
                taskCount--;

                if(logCompletion)
                {
                    Console.WriteLine($"[{workCount - taskCount}/{workCount}] {fileName}");
                }
            }
        });
    }
}
