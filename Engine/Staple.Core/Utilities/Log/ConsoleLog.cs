using System;

namespace Staple;

/// <summary>
/// Console-based logger
/// </summary>
internal class ConsoleLog : ILog
{
    public void Debug(string message)
    {
        Platform.platformProvider.ConsoleLog($"[Debug] {message}");
    }

    public void Error(string message)
    {
        Platform.platformProvider.ConsoleLog($"[Error] {message}");
    }

    public void Info(string message)
    {
        Platform.platformProvider.ConsoleLog($"[Info] {message}");
    }

    public void Warning(string message)
    {
        Platform.platformProvider.ConsoleLog($"[Warning] {message}");
    }

    public void Cleanup()
    {
    }
}