using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple.Player.Windows;

internal class WindowsPlatformProvider : IPlatformProvider
{
    public string StorageBasePath
    {
        get
        {
            var pieces = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace('\\', '/').Split('/').ToList();

            pieces[^1] = "LocalLow";

            return string.Join('/', pieces);
        }
    }

    public IRenderWindow CreateWindow() => new SDL3RenderWindow();

    public void ConsoleLog(object message) => Console.WriteLine($"{message}");

    public Stream OpenFile(string path) => File.OpenRead(path);
}
