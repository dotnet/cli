﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Run
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            app.Name = "dotnet run";
            app.FullName = ".NET Executor";
            app.Description = "Runner for the .NET Platform";
            app.HelpOption("-h|--help");

            var framework = app.Option("-f|--framework <FRAMEWORK>", "Compile a specific framework", CommandOptionType.MultipleValue);
            var configuration = app.Option("-c|--configuration <CONFIGURATION>", "Configuration under which to build", CommandOptionType.SingleValue);
            var preserveTemporaryOutput = app.Option("-t|--preserve-temporary", "Keep the output's temporary directory around", CommandOptionType.NoValue);

            // This is required to be an option because otherwise we can't tell if the first argument is a project or the first argument to pass to an application
            var project = app.Option("-p|--project <PROJECT_PATH>", "The path to the project to run (defaults to the current directory). Can be a path to a project.json or a project directory.", CommandOptionType.SingleValue);

            var noRestore = app.Option("--no-restore", "Do not restore packages before the build.", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                // Locate the project and get the name and full path
                var path = project.Value();
                if (!string.IsNullOrEmpty(path))
                {
                    if (File.Exists(path) && (Path.GetExtension(path) == ".csx"))
                    {
                        return RunInteractive(path);
                    }
                }
                else
                {
                    path = Directory.GetCurrentDirectory();
                }

                var contexts = ProjectContext.CreateContextForEachFramework(path);
                ProjectContext context;
                if (!framework.HasValue())
                {
                    context = contexts.First();
                }
                else
                {
                    var fx = NuGetFramework.Parse(framework.Value());
                    context = contexts.FirstOrDefault(c => c.TargetFramework.Equals(fx));
                }
                return Run(context, configuration.Value() ?? Constants.DefaultConfiguration, app.RemainingArguments, preserveTemporaryOutput.HasValue(), !noRestore.HasValue());
            });

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

        private static int Run(ProjectContext context, string configuration, List<string> remainingArguments, bool preserveTemporaryOutput, bool restore)
        {
            // Create a temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            if (restore)
            {
                Reporter.Verbose.WriteLine("Restoring Nuget packages...");
                var restoreResult = Command.Create($"dotnet-restore", "")
                    .ForwardStdOut(onlyIfVerbose: true)
                    .ForwardStdErr()
                    .Execute();

                if (restoreResult.ExitCode != 0)
                {
                    return restoreResult.ExitCode;
                }
            }
            else
            {
                Reporter.Verbose.WriteLine("Restoring Nuget packages skipped.");
            }

            // Compile to that directory
            var result = Command.Create($"dotnet-compile", $"--output \"{tempDir}\" --temp-output \"{tempDir}\" --framework \"{context.TargetFramework}\" --configuration \"{configuration}\" {context.ProjectFile.ProjectDirectory}")
                .ForwardStdOut(onlyIfVerbose: true)
                .ForwardStdErr()
                .Execute();

            if (result.ExitCode != 0)
            {
                return result.ExitCode;
            }

            // Now launch the output and give it the results
            var outputName = Path.Combine(tempDir, context.ProjectFile.Name + Constants.ExeSuffix);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (context.TargetFramework.IsDesktop())
                {
                    // Run mono if we're running a desktop target on non windows
                    remainingArguments.Insert(0, outputName + ".exe");

                    if (string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase))
                    {
                        // If we're compiling for the debug configuration then add the --debug flag
                        // other options may be passed using the MONO_OPTIONS env var
                        remainingArguments.Insert(0, "--debug");
                    }

                    outputName = "mono";
                }
            }

            // Locate the runtime
            string runtime = Environment.GetEnvironmentVariable("DOTNET_HOME");
            if (string.IsNullOrEmpty(runtime))
            {
                // Use the runtime deployed with the tools, if present
                var candidate = Path.Combine(AppContext.BaseDirectory, "..", "runtime");
                if (File.Exists(Path.Combine(candidate, Constants.LibCoreClrName)))
                {
                    runtime = Path.GetFullPath(candidate);
                }
            }

            result = Command.Create(outputName, string.Join(" ", remainingArguments))
                .ForwardStdOut()
                .ForwardStdErr()
                .EnvironmentVariable("DOTNET_HOME", runtime)
                .Execute();

            // Clean up
            if (!preserveTemporaryOutput)
            {
                Directory.Delete(tempDir, recursive: true);
            }

            return result.ExitCode;
        }

        private static int RunInteractive(string scriptName)
        {
            var command = Command.Create($"dotnet-repl-csi", scriptName)
                .ForwardStdOut()
                .ForwardStdErr();
            var result = command.Execute();
            return result.ExitCode;
        }
    }
}
