using System;

namespace Staple;

/// <summary>
/// Console-based logger
/// </summary>
internal class ConsoleLog : ILog
{
    public void Debug(string message)
    {
        Console.WriteLine($"[Debug] {message}");
    }

    public void Error(string message)
    {
        Console.WriteLine($"[Error] {message}");
    }

    public void Info(string message)
    {
        Console.WriteLine($"[Info] {message}");
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[Warning] {message}");
    }

    public void Cleanup()
    {
    }
}