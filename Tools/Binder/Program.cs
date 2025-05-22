using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;

namespace Binder;

internal class Program
{
    public class Binder : ILibrary
    {
        public class Module
        {
            public string name;
            public List<string> includeDirs = [];
            public List<string> headers = [];
            public List<string> libraryDirs = [];
            public List<string> libraries = [];
            public List<string> defines = [];
            public Dictionary<string, string> replaceStrings = [];
        }

        public List<Module> modules = [];
        public string outputDirectory;

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;

            options.GeneratorKind = GeneratorKind.CSharp;
            options.OutputDir = outputDirectory;

            foreach(var m in modules)
            {
                var module = options.AddModule(m.name);

                foreach(var d in m.defines)
                {
                    module.Defines.Add(d);
                }

                foreach(var dir in m.includeDirs)
                {
                    module.IncludeDirs.Add(dir);
                }

                foreach (var header in m.headers)
                {
                    module.Headers.Add(header);
                }

                foreach (var dir in m.libraryDirs)
                {
                    module.LibraryDirs.Add(dir);
                }

                foreach (var lib in m.libraries)
                {
                    module.Libraries.Add(lib);
                }
            }
        }

        public void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
            driver.Context.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
        }
    }

    static void Main(string[] args)
    {
        if(args.Length == 0)
        {
            Console.WriteLine("CSharp Binder Generator");
            Console.WriteLine("Usage");
            Console.WriteLine("<app> -m moduleName -id includeDir -i includefile -ld libdir -l libfile -d define -rs originalstring replacestring -o outputdir");

            return;
        }

        var binder = new Binder();

        Binder.Module currentModule = null;

        for(var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch(arg)
            {
                case "-m":

                    if(i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing module name");

                        Environment.Exit(1);
                    }

                    currentModule = new()
                    {
                        name = args[i + 1]
                    };

                    binder.modules.Add(currentModule);

                    i++;

                    break;

                case "-id":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing include directory");

                        Environment.Exit(1);
                    }

                    if(currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.includeDirs.Add(args[i + 1]);

                    i++;

                    break;

                case "-i":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing include path");

                        Environment.Exit(1);
                    }

                    if (currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.headers.Add(args[i + 1]);

                    i++;

                    break;

                case "-ld":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing library directory");

                        Environment.Exit(1);
                    }

                    if (currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.libraryDirs.Add(args[i + 1]);

                    i++;

                    break;

                case "-l":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing library path");

                        Environment.Exit(1);
                    }

                    if (currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.libraries.Add(args[i + 1]);

                    i++;

                    break;

                case "-d":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing define");

                        Environment.Exit(1);
                    }

                    if (currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.defines.Add(args[i + 1]);

                    i++;

                    break;

                case "-rs":

                    if (i + 2 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing from/to string args");

                        Environment.Exit(1);
                    }

                    if (currentModule == null)
                    {
                        Console.WriteLine($"Error: No module specified");

                        Environment.Exit(1);
                    }

                    currentModule.replaceStrings.Add(args[i + 1], args[i + 2]);

                    i+=2;

                    break;

                case "-o":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine($"Error: Missing output dir");

                        Environment.Exit(1);
                    }

                    binder.outputDirectory = args[i + 1];

                    i++;

                    break;

                default:

                    Console.WriteLine($"Unknown argument: {arg}");

                    Environment.Exit(1);

                    break;
            }
        }

        ConsoleDriver.Run(binder);

        foreach(var module in binder.modules)
        {
            try
            {
                var path = Path.Combine(binder.outputDirectory ?? AppContext.BaseDirectory, $"{module.name}.cs");

                var text = File.ReadAllText(path);

                foreach(var pair in module.replaceStrings)
                {
                    text = text.Replace(pair.Key, pair.Value);
                }

                File.WriteAllText(path, text);
            }
            catch(Exception)
            {
            }
        }
    }
}
