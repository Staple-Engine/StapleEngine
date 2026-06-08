using Staple.Internal;
using System;
using System.IO;

namespace Staple.Player.Linux;

internal class LinuxPlatformProvider : IPlatformProvider
{
    public string StorageBasePath
    {
        get
        {
            var homePath = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ??
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var basePath = Path.Combine(homePath, ".config", "StapleEngine");

            StorageUtils.CreateDirectory(basePath);

            return basePath;
        }
    }

    public IRenderWindow CreateWindow() => new SDL3RenderWindow();

    public void ConsoleLog(object message) => Console.WriteLine($"{message}");

    public Stream OpenFile(string path) => File.OpenRead(path);
}
