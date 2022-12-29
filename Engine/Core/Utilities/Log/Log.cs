using System;
using System.Runtime.CompilerServices;

namespace Staple
{
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

        internal static Log Instance
        {
            get;
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLog(ILog log)
        {
            Instance = new Log(log);
        }

        public static LogFormat Format = LogFormat.DateTime;

        public static LogType AllowedLogTypes = LogType.Info | LogType.Warning | LogType.Error | LogType.Debug;

        private readonly ILog impl;

        internal Action<LogType, string> onLog;

        internal Log(ILog impl)
        {
            this.impl = impl;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string message)
        {
            if(AllowedLogTypes.HasFlag(LogType.Info) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Info(message);

            Instance?.onLog?.Invoke(LogType.Info, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warning(string message)
        {
            if (AllowedLogTypes.HasFlag(LogType.Warning) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Warning(message);

            Instance?.onLog?.Invoke(LogType.Warning, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string message)
        {
            if (AllowedLogTypes.HasFlag(LogType.Error) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl?.Error(message);

            Instance?.onLog?.Invoke(LogType.Error, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string message)
        {
            if (AllowedLogTypes.HasFlag(LogType.Debug) == false)
            {
                return;
            }

            FormatMessage(ref message);

            Instance?.impl.Debug(message);

            Instance?.onLog?.Invoke(LogType.Debug, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FormatMessage(ref string message)
        {
            switch(Format)
            {
                case LogFormat.Normal:

                    break;

                case LogFormat.DateTime:

                    message = $"[{DateTime.Now:MM/dd/yyyy HH:mm:ss}] {message}";

                    break;
            }
        }
    }
}
