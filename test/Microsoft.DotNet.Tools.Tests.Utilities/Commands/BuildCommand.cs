// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public sealed class BuildCommand : TestCommand
    {

        private bool _captureOutput;

        private string _configuration;

        private NuGetFramework _framework;

        private bool _noDependencies;

        private DirectoryInfo _outputPath;

        private DirectoryInfo _projectDirectory;
        
        private FileInfo _projectFile;

        public BuildCommand()
            : base("dotnet")
        {
        }

        public override CommandResult Execute(string args = "")
        {
            args = $"build {GetNoDependencies()} {GetProjectFile()} {GetProjectDirectory()} {GetOutputPath()} {GetConfiguration()} {GetFramework()} {args}";

            if (_captureOutput)
            {
                return base.ExecuteWithCapturedOutput(args);
            }
            else
            {
                return base.Execute(args);
            }
        }

        public BuildCommand WithCapturedOutput()
        {
            _captureOutput = true;

            return this;
        }

        public BuildCommand WithConfiguration(string configuration)
        {
            _configuration = configuration;

            return this;
        }

        public BuildCommand WithFramework(NuGetFramework framework)
        {
            _framework = framework;

            return this;
        }

        public BuildCommand WithNoDependencies()
        {
            _noDependencies = true;

            return this;
        }

        public BuildCommand WithOutputPath(DirectoryInfo outputPath)
        {
            _outputPath = outputPath;

            return this;
        }

        public BuildCommand WithProjectFile(FileInfo projectFile)
        {
            _projectFile = projectFile;

            return this;
        }

        public BuildCommand WithProjectDirectory(DirectoryInfo projectDirectory)
        {
            _projectDirectory = projectDirectory;

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

            return _framework.ToString();
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

        private string GetProjectDirectory()
        {
            if (_projectDirectory == null)
            {
                return null;
            }

            return $"\"{_projectDirectory.FullName}\"";
        }
    }
}
