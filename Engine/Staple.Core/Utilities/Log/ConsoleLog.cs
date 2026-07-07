namespace Staple;

/// <summary>
/// Console-based logger
/// </summary>
internal class ConsoleLog : ILog
{
    public void Debug(string message)
    {
        Platform.ConsoleLog($"[Debug] {message}");
    }

    public void Error(string message)
    {
        Platform.ConsoleLog($"[Error] {message}");
    }

    public void Info(string message)
    {
        Platform.ConsoleLog($"[Info] {message}");
    }

    public void Warning(string message)
    {
        Platform.ConsoleLog($"[Warning] {message}");
    }

    public void Cleanup()
    {
    }
}