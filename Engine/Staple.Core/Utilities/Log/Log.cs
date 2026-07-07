using System;
using System.Runtime.CompilerServices;
using System.Threading;

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
    private static readonly Lock lockObject = new();

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
    /// <param name="tag">The tag to display, if any</param>
    /// <remarks>The result of using a tag will be similar to: "[tag] message"</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(object message, string tag = null)
    {
        lock (lockObject)
        {
            if (!AllowedLogTypes.HasFlag(LogType.Info) ||
                Platform.suppressLogging)
            {
                return;
            }

            var result = FormatMessage(message, tag);

            Instance?.impl?.Info(result);

            Instance?.onLog?.Invoke(LogType.Info, result);
        }
    }

    /// <summary>
    /// Logs a Warning message.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="tag">The tag to display, if any</param>
    /// <remarks>The result of using a tag will be similar to: "[tag] message"</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(object message, string tag = null)
    {
        lock (lockObject)
        {
            if (!AllowedLogTypes.HasFlag(LogType.Warning) ||
                Platform.suppressLogging)
            {
                return;
            }

            var result = FormatMessage(message, tag);

            Instance?.impl?.Warning(result);

            Instance?.onLog?.Invoke(LogType.Warning, result);
        }
    }

    /// <summary>
    /// Logs an Error message.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="tag">The tag to display, if any</param>
    /// <remarks>The result of using a tag will be similar to: "[tag] message"</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(object message, string tag = null)
    {
        lock (lockObject)
        {
            if (!AllowedLogTypes.HasFlag(LogType.Error) ||
                Platform.suppressLogging)
            {
                return;
            }

            var result = FormatMessage(message, tag);

            Instance?.impl?.Error(result);

            Instance?.onLog?.Invoke(LogType.Error, result);
        }
    }

    /// <summary>
    /// Logs a Debug message.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="tag">The tag to display, if any</param>
    /// <remarks>The result of using a tag will be similar to: "[tag] message"</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(object message, string tag = null)
    {
        lock (lockObject)
        {
            if (!AllowedLogTypes.HasFlag(LogType.Debug) ||
                Platform.suppressLogging)
            {
                return;
            }

            var result = FormatMessage(message, tag);

            Instance?.impl.Debug(result);

            Instance?.onLog?.Invoke(LogType.Debug, result);
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
    private static string FormatMessage(object message, string tag)
    {
        var messageString = message?.ToString() ?? "(null)";

        if(!string.IsNullOrWhiteSpace(tag))
        {
            messageString = $"[{tag}] {messageString}";
        }

        return Format switch
        {
            LogFormat.Normal => messageString,
            LogFormat.DateTime => $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}] {messageString}",
            _ => "(null)",
        };
    }
}
