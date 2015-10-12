using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Cli
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "dotnet",
                FullName = ".NET Driver",
                Description = "Command Line Driver for the .NET Platform"
            };
            app.HelpOption("-h|--help");

            app.OnExecute(() =>
            {
                if (args.Length > 0)
                {
                    var exitCode = CreateCommand(args[0], args.Skip(1));
                    if (exitCode != 0)
                    {
                        app.ShowHelp();
                    }
                    return exitCode;
                }
                app.ShowHelp();
                return 0;
            });

            const string commandName = "commands";
            app.Command(commandName, c =>
            {
                c.Description = "List all commands";

                c.HelpOption("-h|--help");

                // TODO: Build the 'dotnet-commands' command to list all possible commands
                c.OnExecute(() => CreateCommand(commandName, args));
            });

            try
            {
                return app.Execute(args);
            }
            catch (OperationCanceledException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int CreateCommand(string commandName, IEnumerable<string> args)
        {
            return Command.Create("dotnet-" + commandName, args.ToArray())
                .ForwardStdErr()
                .ForwardStdOut()
                .RunAsync()
                .Result
                .ExitCode;
        }
    }
}
