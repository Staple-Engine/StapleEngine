using System;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Main Logging class
/// </summary>
public class Log
{
    public enum LogFormat
    {
        Normal,
        DateTime
    }

    [Flags]
    public enum LogType
    {
        Info = (1 << 0),
        Warning = (1 << 1),
        Error = (1 << 2),
        Debug = (1 << 3),
    }

    /// <summary>
    /// The default logging instance
    /// </summary>
    internal static Log Instance
    {
        get;
        private set;
    }

    /// <summary>
    /// Changes the current log implementation
    /// </summary>
    /// <param name="log"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetLog(ILog log)
    {
        Instance = new Log(log);
    }

    /// <summary>
    /// The current log format
    /// </summary>
    public static LogFormat Format = LogFormat.DateTime;

    /// <summary>
    /// The allowed types to log
    /// </summary>
    public static LogType AllowedLogTypes = LogType.Info | LogType.Warning | LogType.Error | LogType.Debug;

    /// <summary>
    /// The implementation of the log interface
    /// </summary>
    private readonly ILog impl;

    /// <summary>
    /// Thread lock
    /// </summary>
    private static object lockObject = new();

    /// <summary>
    /// Event for when a message is logged
    /// </summary>
    internal Action<LogType, string> onLog;

    /// <summary>
    /// Internal constructor with an implementation.
    /// </summary>
    /// <param name="impl">The implementation to use.</param>
    internal Log(ILog impl)
    {
        this.impl = impl;
    }

    /// <summary>
    /// Logs an Info message.
    /// </summary>
    /// <param name="message">The message to log</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message)
    {
        lock (lockObject)
        {
            if (AllowedLogTypes.HasFlag(LogType.Info) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Info(message);

            Instance?.onLog?.Invoke(LogType.Info, message);
        }
    }

    /// <summary>
    /// Logs a Warning message.
    /// </summary>
    /// <param name="message">The message to log</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(string message)
    {
        lock (lockObject)
        {
            if (AllowedLogTypes.HasFlag(LogType.Warning) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Warning(message);

            Instance?.onLog?.Invoke(LogType.Warning, message);
        }
    }

    /// <summary>
    /// Logs an Error message.
    /// </summary>
    /// <param name="message">The message to log</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
    {
        lock (lockObject)
        {
            if (AllowedLogTypes.HasFlag(LogType.Error) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Error(message);

            Instance?.onLog?.Invoke(LogType.Error, message);
        }
    }

    /// <summary>
    /// Logs a Debug message.
    /// </summary>
    /// <param name="message">The message to log</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string message)
    {
        lock (lockObject)
        {
            if (AllowedLogTypes.HasFlag(LogType.Debug) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl.Debug(message);

            Instance?.onLog?.Invoke(LogType.Debug, message);
        }
    }

    public static void Cleanup()
    {
        Instance?.impl.Cleanup();
    }

    /// <summary>
    /// Formats a message based on the log format
    /// </summary>
    /// <param name="message">The message to format</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FormatMessage(ref string message)
    {
        switch(Format)
        {
            case LogFormat.Normal:

                break;

            case LogFormat.DateTime:

                message = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}] {message}";

                break;
        }
    }
}
