using System;

namespace Staple.Jobs;

public class ActionJob(Action action) : IJob
{
    public Action action = action;
    
    public void Execute()
    {
        action();
    }
}
