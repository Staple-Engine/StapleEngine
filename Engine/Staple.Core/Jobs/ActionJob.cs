using System;

namespace Staple.Jobs;

public class ActionJob(Action action, Action<Exception> exception = null) : IJob
{
    public Action action = action;

    public Action<Exception> exception = exception;
    
    public void Execute()
    {
        action();
    }

    public void Failure(Exception e)
    {
        exception?.Invoke(e);
    }
}
