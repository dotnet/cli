// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.TestHost;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.TestHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // We want to allow unexpected args, in case future VS needs to pass anything in that we don't current.
            // This will allow us to retain backwards compatibility.
            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            application.HelpOption("-?|-h|--help");

            var env = PlatformServices.Default.Application;

            var portOption = application.Option(
                "--port",
                "Port number to listen for a connection.",
                CommandOptionType.SingleValue);

            var projectOption = application.Option(
                "--project",
                "Path to a project file.",
                CommandOptionType.SingleValue);

            var debugOption = application.Option("--debug", "Launch the debugger", CommandOptionType.NoValue);

            var waitOption = application.Option("--wait", "Wait for attach", CommandOptionType.NoValue);

            var parentProcessIdOption = application.Option(
                "--parentProcessId",
                "Process id of the parent which launched this process",
                CommandOptionType.SingleValue);

            // If no command was specified at the commandline, then wait for a command via message.
            application.OnExecute(async () =>
            {
                // Register for parent process's exit event
                var parentProcessId = parentProcessIdOption.Value();
                if (!string.IsNullOrEmpty(parentProcessId))
                {
                    int id;
                    if (!Int32.TryParse(parentProcessId, out id))
                    {
                        TestHostTracing.Source.TraceEvent(
                            TraceEventType.Error,
                            0,
                            $"Invalid process id '{id}'. Process id must be an integer.");
                        return -1;
                    }

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

                if (debugOption.HasValue())
                {
                    Debugger.Launch();
                }

                if (waitOption.HasValue())
                {
                    Thread.Sleep(10 * 1000);
                }

                var projectPath = projectOption.Value() ?? env.ApplicationBasePath;
                var port = int.Parse(portOption.Value());

                Console.WriteLine("Listening on port {0}", port);
                using (var channel = ReportingChannel.ListenOn(port))
                {
                    Console.WriteLine("Client accepted {0}", channel.Socket.LocalEndPoint);

                    try
                    {
                        string testCommand = null;
                        Project project = null;
                        if (Project.TryGetProject(projectPath, out project))
                        {
                            project.Commands.TryGetValue("test", out testCommand);
                        }

                        if (testCommand == null)
                        {
                            // No test command means no tests.
                            TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, "Project has no test command.");
                            channel.Send(new Message()
                            {
                                MessageType = "TestExecution.Response",
                            });
                            return -1;
                        }

                        var message = channel.ReadQueue.Take();

                        // The message might be a request to negotiate protocol version. For now we only know
                        // about version 1.
                        if (message.MessageType == "ProtocolVersion")
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

                            // Take the next message, which should be the command to execute.
                            message = channel.ReadQueue.Take();
                        }

                        if (message.MessageType == "TestDiscovery.Start")
                        {
                            TestHostTracing.Source.TraceInformation("Starting Discovery");
                            var commandArgs = new string[]
                            {
                                "--list",
                                "--designtime"
                            };

                            var services = new ProjectTestHostServices(project, channel);
                            await ProjectCommand.Execute(project, services, "test", commandArgs.ToArray());

                            channel.Send(new Message()
                            {
                                MessageType = "TestDiscovery.Response",
                            });

                            TestHostTracing.Source.TraceInformation("Completed Discovery");
                            return 0;
                        }
                        else if (message.MessageType == "TestExecution.Start")
                        {
                            TestHostTracing.Source.TraceInformation("Starting Execution");
                            var commandArgs = new List<string>()
                            {
                                "--designtime"
                            };

                            var tests = message.Payload?.ToObject<RunTestsMessage>().Tests;
                            if (tests != null)
                            {
                                foreach (var test in tests)
                                {
                                    commandArgs.Add("--test");
                                    commandArgs.Add(test);
                                }
                            }

                            var services = new ProjectTestHostServices(project, channel);
                            await ProjectCommand.Execute(project, services, "test", commandArgs.ToArray());

                            channel.Send(new Message()
                            {
                                MessageType = "TestExecution.Response",
                            });

                            TestHostTracing.Source.TraceInformation("Completed Execution");
                            return 0;
                        }
                        else
                        {
                            var error = string.Format("Unexpected message type: '{0}'.", message.MessageType);
                            TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, error);
                            channel.SendError(error);
                            return -1;
                        }
                    }
                    catch (Exception ex)
                    {
                        TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, ex.ToString());
                        channel.SendError(ex);
                        return -2;
                    }
                }
            });

            return application.Execute(args);
        }
    }
}