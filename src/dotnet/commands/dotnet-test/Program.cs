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

namespace Microsoft.DotNet.Tools.Test
{
    public class TestCommand
    {
        private readonly IDotnetTestRunnerFactory _dotnetTestRunnerFactory;

        public TestCommand(IDotnetTestRunnerFactory testRunnerFactory)
        {
            _dotnetTestRunnerFactory = testRunnerFactory;
        }

        public int DoRun(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var dotnetTestParams = new DotnetTestParams();

            dotnetTestParams.Parse(args);

            try
            {
                if (dotnetTestParams.Help)
                {
                    return 0;
                }

                // Register for parent process's exit event
                if (dotnetTestParams.ParentProcessId.HasValue)
                {
                    RegisterForParentProcessExit(dotnetTestParams.ParentProcessId.Value);
                }

                var projectContexts = CreateProjectContexts(dotnetTestParams.ProjectPath, dotnetTestParams.Runtime);
                var ranTests = false;
                foreach (var projectContext in projectContexts)
                {
                    if (dotnetTestParams.Framework != null && dotnetTestParams.Framework != projectContext.TargetFramework)
                    {
                        continue;
                    }

                    ranTests = true;
                    var testRunner = projectContext.ProjectFile.TestRunner;
                    var dotnetTestRunner = _dotnetTestRunnerFactory.Create(dotnetTestParams.Port);
                    var result = dotnetTestRunner.RunTests(projectContext, dotnetTestParams);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                if (!ranTests && projectContexts.Any() && dotnetTestParams.Framework != null)
                {
                    TestHostTracing.Source.TraceEvent(
                        TraceEventType.Error, 
                        0, 
                        $"The target framework {dotnetTestParams.Framework} does not exist in {dotnetTestParams.ProjectPath}.");
                    return 1;
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
        }

        public static int Run(string[] args)
        {
            var testCommand = new TestCommand(new DotnetTestRunnerFactory());

            return testCommand.DoRun(args);
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

        private static IEnumerable<ProjectContext> CreateProjectContexts(string projectPath, string runtimeIdentifier)
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

            var runtimeIdentifiers = string.IsNullOrEmpty(runtimeIdentifier) ? null : new[] { runtimeIdentifier };
            return ProjectContext.CreateContextForEachFramework(projectPath, runtimeIdentifiers: runtimeIdentifiers);
        }
    }
}