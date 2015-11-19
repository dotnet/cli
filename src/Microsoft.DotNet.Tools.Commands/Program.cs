using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;

namespace Microsoft.DotNet.Tools.Commands
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            
            var app = SetupApp();
            return Execute(app, args);
        }
        
        private static CommandLineApplication SetupApp()
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet commands";
            app.FullName = ".NET Commands Listing";
            app.Description = "List all available commands in the dotnet tool";
            app.HelpOption("-h|--help");

            app.OnExecute(() =>
            {
                PrintCommandList();
                
                return 0;
            });
            
            return app;
        }
        
        private static int Execute(CommandLineApplication app, string[] args)
        {
            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.Error.WriteLine(ex);
#else
                Reporter.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }
        
        private static void PrintCommandList()
        {
            var availableCommands = GetAvailableCommands();
            availableCommands.Sort();
            
            PrintHeader();
            
            foreach (var command in availableCommands)
            {
                System.Console.WriteLine(command);
            }
        }
        
        private static void PrintHeader()
        {
            System.Console.WriteLine("dotnet available commands:");
        }
        
        private static List<string> GetAvailableCommands()
        {
            var available = new List<string>();
            
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            var pathDirs = pathValue.Split(Path.PathSeparator);
            
            foreach(var dir in pathDirs)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, Constants.CommandSearchPattern);
                    
                    foreach(var filepath in files)
                    {
                        var filename = Path.GetFileNameWithoutExtension(filepath);
                        var commandName = FileNameToCommandName(filename);
                        
                        available.Add(commandName);
                    }
                }
            }
            
            return available;
        }
        
        private static string FileNameToCommandName(string filename)
        {
             return filename.Replace(Constants.CommandPrefix + "-", "");
        }
        
    }
}
