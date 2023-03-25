using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CrossCopy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = new Process();

                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "cmd.exe",
                        Arguments = $"/C copy /B /Y {string.Join(" ", args)}",
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    };

                    process.StartInfo = startInfo;

                    process.Start();

                    while(process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

                    Environment.Exit(process.ExitCode);
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

                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "/bin/bash",
                        Arguments = $"-lic cp -Rf {string.Join(" ", args)}",
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    };

                    process.StartInfo = startInfo;

                    process.Start();

                    while (process.StandardOutput.EndOfStream == false)
                    {
                        var str = process.StandardOutput.ReadLine();

                        Console.WriteLine(str);
                    }

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