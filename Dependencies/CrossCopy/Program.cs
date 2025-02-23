using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CrossCopy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: CrossCopy source destination");

                Console.WriteLine($"Debug: Arguments passed:\n{string.Join("\n", args)}");

                Environment.Exit(1);
            }

            var dllExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" : "so";

            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                dllExt = "dylib";
            }

            for(var i = 0; i < args.Length; i++)
            {
                args[i] = args[i]
					.Replace("[DLL]", dllExt)
					.Replace('\\', Path.DirectorySeparatorChar)
					.Replace('/', Path.DirectorySeparatorChar);
            }

            try
            {
                Directory.CreateDirectory(args[1]);
            }
            catch(Exception)
            {
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = new Process();

                    var commandArgs = "";
                    var isDirectory = false;

                    if (Directory.Exists(args[0]))
                    {
                        isDirectory = true;

                        var directoryName = Path.GetFileName(args[0]);

                        if (args[1].EndsWith(directoryName) == false)
                        {
                            args[1] += $"{Path.DirectorySeparatorChar}{directoryName}";
                        }
                    }

                    var argsString = "";

                    foreach (var arg in args)
                    {
                        argsString += $"\"{arg}\" ";
                    }

                    if (isDirectory)
                    {
                        commandArgs = $"/C robocopy {argsString} /E /NJH /NJS /NP /NS /NC";
                    }
                    else
                    {
                        commandArgs = $"/C copy /B /Y {argsString}";
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = commandArgs,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    };

                    Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");

                    process.StartInfo = startInfo;

                    process.Start();

                    while(process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

                    process.WaitForExit();

                    Environment.Exit(isDirectory && process.ExitCode < 8 ? 0 : process.ExitCode);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());

                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    var process = new Process();

                    var argsString = "";

                    foreach(var arg in args)
                    {
                        argsString += $"{arg.Replace(" ", "\\ ")} ";
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-lc \"cp -Rf {argsString}\"",
                        RedirectStandardOutput = true,
                    };

                    Console.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");

                    process.StartInfo = startInfo;

                    process.Start();

                    while(process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

                    process.WaitForExit();

                    Environment.Exit(process.ExitCode);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());

                    Environment.Exit(1);
                }
            }
        }
    }
}