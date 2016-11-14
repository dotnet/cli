﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class BuildPJCommand : TestCommand
    {

        private bool _captureOutput;

        private string _configuration;

        private NuGetFramework _framework;

        private string _runtime;

        private bool _noDependencies;

        private DirectoryInfo _outputPath;
        
        private FileInfo _projectFile;

        private DirectoryInfo _workingDirectory;

        public BuildPJCommand()
            : base(new RepoDirectoriesProvider().PjDotnet)
        {
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"build {GetNoDependencies()} {GetProjectFile()} {GetOutputPath()} {GetConfiguration()} {GetFramework()} {GetRuntime()} {args}";

            if (_workingDirectory != null)
            {
                this.WithWorkingDirectory(_workingDirectory.FullName);
            }
            
            if (_captureOutput)
            {
                return base.ExecuteWithCapturedOutput(args);
            }
            else
            {
                return base.Execute(args);
            }
        }

        public override CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            WithCapturedOutput();

            return Execute(args);
        }

        public BuildPJCommand WithCapturedOutput()
        {
            _captureOutput = true;

            return this;
        }

        public BuildPJCommand WithConfiguration(string configuration)
        {
            _configuration = configuration;

            return this;
        }

        public BuildPJCommand WithFramework(NuGetFramework framework)
        {
            _framework = framework;

            return this;
        }

        public BuildPJCommand WithRuntime(string runtime)
        {
            _runtime = runtime;

            return this;
        }

        public BuildPJCommand WithNoDependencies()
        {
            _noDependencies = true;

            return this;
        }

        public BuildPJCommand WithOutputPath(DirectoryInfo outputPath)
        {
            _outputPath = outputPath;

            return this;
        }

        public BuildPJCommand WithProjectDirectory(DirectoryInfo projectDirectory)
        {
            _workingDirectory = projectDirectory;

            return this;
        }

        public BuildPJCommand WithProjectFile(FileInfo projectFile)
        {
            _projectFile = projectFile;

            return this;
        }

        public BuildPJCommand WithWorkingDirectory(DirectoryInfo workingDirectory)
        {
            _workingDirectory = workingDirectory;

            return this;
        }

        private string GetConfiguration()
        {
            if (_configuration == null)
            {
                return null;
            }

            return $"--configuration {_configuration}";
        }

        private string GetFramework()
        {
            if (_framework == null)
            {
                return null;
            }

            return $"--framework {_framework.GetShortFolderName()}";
        }

        private string GetRuntime()
        {
            if (_runtime == null)
            {
                return null;
            }

            return $"--runtime {_runtime}";
        }

        private string GetNoDependencies()
        {
            if (!_noDependencies)
            {
                return null;
            }

            return "--no-dependencies";
        }

        private string GetOutputPath()
        {
            if (_outputPath == null)
            {
                return null;
            }

            return $"\"{_outputPath.FullName}\"";
        }

        private string GetProjectFile()
        {
            if (_projectFile == null)
            {
                return null;
            }

            return $"\"{_projectFile.FullName}\"";
        }
    }
}
