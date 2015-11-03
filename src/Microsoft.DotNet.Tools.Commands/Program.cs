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
                Console.Error.WriteLine(ex.Message);
#endif
                return 1;
            }
        }
        
        private static void PrintCommandList()
        {
            List<string> availableCommands = GetAvailableCommands();
            availableCommands.Sort();
            
            PrintHeader();
            
            foreach (string command in availableCommands)
            {
                System.Console.WriteLine(command);
            }
        }
        
        private static void PrintHeader()
        {
            System.Console.WriteLine("Dotnet Tool Available Commands:");
        }
        
        private static List<string> GetAvailableCommands()
        {
            List<string> available = new List<string>();
            
            string pathValue = Environment.GetEnvironmentVariable("PATH");
            string[] pathDirs = pathValue.Split(Path.PathSeparator);
            
            foreach(string dir in pathDirs)
            {
                string[] files = Directory.GetFiles(dir, Constants.CommandSearchPattern);
                
                foreach(string filepath in files){
                    string filename = Path.GetFileNameWithoutExtension(filepath);
                    string commandName = FileNameToCommandName(filename);
                    
                    available.Add(commandName);
                }
            }
            
            return available;
        }
        
        private static string FileNameToCommandName(string filename)
        {
             return filename.Replace(Constants.CommandPrefix, "");
        }
        
    }
}
