using System;
using System.IO;

namespace Staple
{
    /// <summary>
    /// Filesystem-based logger
    /// </summary>
    internal class FSLog : ILog
    {
        private readonly StreamWriter writer;
        private readonly FileStream stream;

        public FSLog(string path)
        {
            try
            {
                stream = File.Open(path, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
            }
            catch (Exception)
            {
                return;
            }
        }

        public void Debug(string message)
        {
            writer?.WriteLine($"[Debug] {message}");
        }

        public void Error(string message)
        {
            writer?.WriteLine($"[Error] {message}");
        }

        public void Info(string message)
        {
            writer?.WriteLine($"[Info] {message}");
        }

        public void Warning(string message)
        {
            writer?.WriteLine($"[Warning] {message}");
        }
        public void Cleanup()
        {
            writer?.Dispose();
            stream?.Dispose();
        }
    }
}