﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.MSBuild;

namespace Microsoft.DotNet.Tools.Run
{
    public partial class RunCommand
    {
        public string Configuration { get; set; }
        public string Framework { get; set; }
        public string Project { get; set; }
        public IReadOnlyList<string> Args { get; set; }

        private List<string> _args;

        public RunCommand()
        {
        }

        public int Start()
        {
            Initialize();

            EnsureProjectIsBuilt();

            ICommand runCommand = GetRunCommand();

            return runCommand
                .Execute()
                .ExitCode;
        }

        private void EnsureProjectIsBuilt()
        {
            List<string> buildArgs = new List<string>();
 
            buildArgs.Add(Project); 
 
            buildArgs.Add("/nologo");
            buildArgs.Add("/verbosity:quiet");

            if (!string.IsNullOrWhiteSpace(Configuration))
            {
                buildArgs.Add($"/p:Configuration={Configuration}");
            }

            if (!string.IsNullOrWhiteSpace(Framework))
            {
                buildArgs.Add($"/p:TargetFramework={Framework}");
            }

            var buildResult = new MSBuildForwardingApp(buildArgs).Execute();

            if (buildResult != 0)
            {
                Reporter.Error.WriteLine();
                throw new GracefulException(LocalizableStrings.RunCommandException);
            }
        }

        private ICommand GetRunCommand()
        {
            var globalProperties = new Dictionary<string, string>
            {
                { Constants.MSBuildExtensionsPath, AppContext.BaseDirectory }
            };

            if (!string.IsNullOrWhiteSpace(Configuration))
            {
                globalProperties.Add("Configuration", Configuration);
            }

            if (!string.IsNullOrWhiteSpace(Framework))
            {
                globalProperties.Add("TargetFramework", Framework);
            }

            ProjectInstance projectInstance = new ProjectInstance(Project, globalProperties, null);

            string runProgram = projectInstance.GetPropertyValue("RunCommand");
            if (string.IsNullOrEmpty(runProgram))
            {
                string outputType = projectInstance.GetPropertyValue("OutputType");

                throw new GracefulException(
                    string.Format(
                        LocalizableStrings.RunCommandExceptionUnableToRun,
                        "dotnet run",
                        "OutputType",
                        outputType));
            }

            string runArguments = projectInstance.GetPropertyValue("RunArguments");
            string runWorkingDirectory = projectInstance.GetPropertyValue("RunWorkingDirectory");

            string fullArguments = runArguments;
            if (_args.Any())
            {
                fullArguments += " " + ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_args);
            }

            CommandSpec commandSpec = new CommandSpec(runProgram, fullArguments, CommandResolutionStrategy.None);

            return Command.Create(commandSpec)
                .WorkingDirectory(runWorkingDirectory);
        }

        private void Initialize()
        {
            if (string.IsNullOrWhiteSpace(Project))
            {
                string directory = Directory.GetCurrentDirectory();
                string[] projectFiles = Directory.GetFiles(directory, "*.*proj");

                if (projectFiles.Length == 0)
                {
                    var project = "--project";

                    throw new InvalidOperationException(
                        $"Couldn't find a project to run. Ensure a project exists in  {directory}, or pass the path to the project using {project}");
                }
                else if (projectFiles.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Specify which project file to use because {directory} contains more than one project file.");
                }

                Project = projectFiles[0];
            }

            if (Args == null)
            {
                _args = new List<string>();
            }
            else
            {
                _args = new List<string>(Args);
            }
        }
    }
}
