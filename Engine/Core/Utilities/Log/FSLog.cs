using System;
using System.IO;

namespace Staple
{
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
            catch (Exception)
            {
                return;
            }
        }

        ~FSLog()
        {
            writer?.Dispose();
            stream?.Dispose();
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
    }
}