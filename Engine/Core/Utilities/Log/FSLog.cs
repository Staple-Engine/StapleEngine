using System;
using System.IO;

namespace Staple;

/// <summary>
/// Filesystem-based logger
/// </summary>
internal class FSLog : ILog
{
    private StreamWriter writer;
    private FileStream stream;

    public FSLog(string path)
    {
        try
        {
            stream = File.Open(path, FileMode.Create, FileAccess.Write);
            writer = new StreamWriter(stream);
        }
        catch (Exception e)
        {
            stream?.Dispose();
            writer?.Dispose();

            stream = null;
            writer = null;

            Console.WriteLine($"[Log] Unable to create log file at {path}, defaulting to console: {e}");

            return;
        }
    }

    public void Debug(string message)
    {
        var m = $"[Debug] {message}";

        writer?.WriteLine(m);

        if(writer == null)
        {
            Console.WriteLine(m);
        }
    }

    public void Error(string message)
    {
        var m = $"[Error] {message}";

        writer?.WriteLine(m);

        if (writer == null)
        {
            Console.WriteLine(m);
        }
    }

    public void Info(string message)
    {
        var m = $"[Info] {message}";

        writer?.WriteLine(m);

        if (writer == null)
        {
            Console.WriteLine(m);
        }
    }

    public void Warning(string message)
    {
        var m = $"[Warning] {message}";

        writer?.WriteLine(m);

        if (writer == null)
        {
            Console.WriteLine(m);
        }
    }

    public void Cleanup()
    {
        writer?.Dispose();
        stream?.Dispose();

        writer = null;
        stream = null;
    }
}