namespace Staple
{
    internal class ConsoleLog : ILog
    {
        public void Debug(string message)
        {
            System.Console.WriteLine($"[Debug] {message}");
        }

        public void Error(string message)
        {
            System.Console.WriteLine($"[Error] {message}");
        }

        public void Info(string message)
        {
            System.Console.WriteLine($"[Info] {message}");
        }

        public void Warning(string message)
        {
            System.Console.WriteLine($"[Warning] {message}");
        }
    }
}