// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.Testing.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Test
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication(false)
            {
                Name = "dotnet test",
                FullName = ".NET Test Driver",
                Description = "Test Driver for the .NET Platform"
            };

            app.HelpOption("-?|-h|--help");

            var parentProcessIdOption = app.Option("--parentProcessId", "Parent process ID.", CommandOptionType.SingleValue);
            var portOption = app.Option("--port", "Port number to listen for a connection.", CommandOptionType.SingleValue);
            var projectPath = app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory.");
            
            app.OnExecute(() =>
            {
                try
                {
                    // Register for parent process's exit event
                    if (parentProcessIdOption.HasValue())
                    {
                        int processId;

                        if (!Int32.TryParse(parentProcessIdOption.Value(), out processId))
                        {
                            throw new InvalidOperationException($"Invalid process id '{parentProcessIdOption.Value()}'. Process id must be an integer.");
                        }

                        RegisterForParentProcessExit(processId);
                    }
                    
                    var projectContexts = CreateProjectContexts(projectPath.Value);

                    var projectContext = projectContexts.First();

                    var testRunner = projectContext.ProjectFile.TestRunner;
                    
                    if (portOption.HasValue())
                    {
                        int port;
                        
                        if (!Int32.TryParse(portOption.Value(), out port))
                        {
                            throw new InvalidOperationException($"{portOption.Value()} is not a valid port number.");
                        }

                        RunDesignTime(port, projectContext, testRunner);
                    }
                    else
                    {
                        RunConsole(projectContext, app, testRunner);
                    }

                    return 0;
                }
                catch (InvalidOperationException ex)
                {
                    TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, ex.ToString());
                    return -1;
                }
                catch (Exception ex)
                {
                    TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, ex.ToString());
                    return -2;
                }

            });

            return app.Execute(args);
        }

        private static void RunConsole(ProjectContext projectContext, CommandLineApplication app, string testRunner)
        {
            var commandArgs = new List<string> {projectContext.AssemblyPath(Constants.DefaultConfiguration)};
            commandArgs.AddRange(app.RemainingArguments);

            Command.Create($"{GetCommandName(testRunner)}", commandArgs, projectContext.TargetFramework)
                .ForwardStdErr()
                .ForwardStdOut()
                .Execute();
        }

        private static void RunDesignTime(int port, ProjectContext projectContext, string testRunner)
        {
            Console.WriteLine("Listening on port {0}", port);
            using (var channel = ReportingChannel.ListenOn(port))
            {
                Console.WriteLine("Client accepted {0}", channel.Socket.LocalEndPoint);

                HandleDesignTimeMessages(projectContext, testRunner, channel);
            }
        }

        private static void HandleDesignTimeMessages(ProjectContext projectContext, string testRunner, ReportingChannel channel)
        {
            try
            {
                var message = channel.ReadQueue.Take();

                if (message.MessageType == "ProtocolVersion")
                {
                    HandleProtocolVersionMessage(message, channel);

                    // Take the next message, which should be the command to execute.
                    message = channel.ReadQueue.Take();
                }

                if (message.MessageType == "TestDiscovery.Start")
                {
                    HandleTestDiscoveryStartMessage(testRunner, channel, projectContext);
                }
                else if (message.MessageType == "TestExecution.Start")
                {
                    HandleTestExecutionStartMessage(testRunner, message, channel, projectContext);
                }
                else
                {
                    HandleUnknownMessage(message, channel);
                }
            }
            catch (Exception ex)
            {
                channel.SendError(ex);
            }
        }

        private static void HandleProtocolVersionMessage(Message message, ReportingChannel channel)
        {
            var version = message.Payload?.ToObject<ProtocolVersionMessage>().Version;
            var supportedVersion = 1;
            TestHostTracing.Source.TraceInformation(
                "[ReportingChannel]: Requested Version: {0} - Using Version: {1}",
                version,
                supportedVersion);

            channel.Send(new Message()
            {
                MessageType = "ProtocolVersion",
                Payload = JToken.FromObject(new ProtocolVersionMessage()
                {
                    Version = supportedVersion,
                }),
            });
        }

        private static void HandleTestDiscoveryStartMessage(string testRunner, ReportingChannel channel, ProjectContext projectContext)
        {
            TestHostTracing.Source.TraceInformation("Starting Discovery");

            var commandArgs = new List<string> { projectContext.AssemblyPath(Constants.DefaultConfiguration) };

            commandArgs.AddRange(new[]
            {
                "--list",
                "--designtime"
            });

            ExecuteRunnerCommand(testRunner, channel, commandArgs);

            channel.Send(new Message()
            {
                MessageType = "TestDiscovery.Response",
            });

            TestHostTracing.Source.TraceInformation("Completed Discovery");
        }

        private static void HandleTestExecutionStartMessage(string testRunner, Message message, ReportingChannel channel, ProjectContext projectContext)
        {
            TestHostTracing.Source.TraceInformation("Starting Execution");

            var commandArgs = new List<string> { projectContext.AssemblyPath(Constants.DefaultConfiguration) };

            commandArgs.AddRange(new[]
            {
                "--designtime"
            });

            var tests = message.Payload?.ToObject<RunTestsMessage>().Tests;
            if (tests != null)
            {
                foreach (var test in tests)
                {
                    commandArgs.Add("--test");
                    commandArgs.Add(test);
                }
            }

            ExecuteRunnerCommand(testRunner, channel, commandArgs);

            channel.Send(new Message()
            {
                MessageType = "TestExecution.Response",
            });

            TestHostTracing.Source.TraceInformation("Completed Execution");
        }

        private static void HandleUnknownMessage(Message message, ReportingChannel channel)
        {
            var error = string.Format("Unexpected message type: '{0}'.", message.MessageType);

            TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, error);

            channel.SendError(error);

            throw new InvalidOperationException(error);
        }
        
        private static void ExecuteRunnerCommand(string testRunner, ReportingChannel channel, List<string> commandArgs)
        {
            var result = Command.Create(GetCommandName(testRunner), commandArgs, new NuGetFramework("DNXCore", Version.Parse("5.0")))
                .OnOutputLine(line =>
                {
                    try
                    {
                        channel.Send(JsonConvert.DeserializeObject<Message>(line));
                    }
                    catch
                    {
                        TestHostTracing.Source.TraceInformation(line);
                    }
                })
                .Execute();

            if (result.ExitCode != 0)
            {
                channel.SendError($"{GetCommandName(testRunner)} returned '{result.ExitCode}'.");
            }
        }

        private static string GetCommandName(string testRunner)
        {
            return $"dotnet-test-{testRunner}";
        }

        private static void RegisterForParentProcessExit(int id)
        {
            var parentProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == id);

            if (parentProcess != null)
            {
                parentProcess.EnableRaisingEvents = true;
                parentProcess.Exited += (sender, eventArgs) =>
                {
                    TestHostTracing.Source.TraceEvent(
                        TraceEventType.Information,
                        0,
                        "Killing the current process as parent process has exited.");

                    Process.GetCurrentProcess().Kill();
                };
            }
            else
            {
                TestHostTracing.Source.TraceEvent(
                    TraceEventType.Information,
                    0,
                    "Failed to register for parent process's exit event. " +
                    $"Parent process with id '{id}' was not found.");
            }
        }

        private static IEnumerable<ProjectContext> CreateProjectContexts(string projectPath)
        {
            projectPath = projectPath ?? Directory.GetCurrentDirectory();

            if (!projectPath.EndsWith(Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Project.FileName);
            }

            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"{projectPath} does not exist.");
            }

            return ProjectContext.CreateContextForEachFramework(projectPath);
        }
    }
}