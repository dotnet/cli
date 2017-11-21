// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    internal class LinuxEnvironmentPath : IEnvironmentPath
    {
        private readonly IFile _fileSystem;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly IReporter _reporter;
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;
        private const string ProfiledDotnetCliToolsPath = @"/etc/profile.d/dotnet-cli-tools-bin-path.sh";

        internal LinuxEnvironmentPath(
            string packageExecutablePath,
            IReporter reporter,
            IEnvironmentProvider environmentProvider,
            IFile fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environmentProvider
                = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _reporter
                = reporter ?? throw new ArgumentNullException(nameof(reporter));
            _packageExecutablePath
                = packageExecutablePath ?? throw new ArgumentNullException(nameof(packageExecutablePath));
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var script = $"export PATH=\"$PATH:{_packageExecutablePath}\"";
            _fileSystem.WriteAllText(ProfiledDotnetCliToolsPath, script);
        }

        private bool PackageExecutablePathExists()
        {
            return _environmentProvider
                .GetEnvironmentVariable(PathName)
                .Split(':').Contains(_packageExecutablePath);
        }

        public void PrintAddPathInstructionIfPathDoesNotExist()
        {
            if (!PackageExecutablePathExists())
            {
                if (_fileSystem.Exists(ProfiledDotnetCliToolsPath))
                {
                    _reporter.WriteLine("You need logout to be able to run new installed command from shell");
                }
                else
                {
                    // similar to https://code.visualstudio.com/docs/setup/mac
                    _reporter.WriteLine(
                        $"Cannot find tools executable path in environement PATH. Please ensure {_packageExecutablePath} is added to your PATH.{Environment.NewLine}" +
                        $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                        $"cat << EOF >> ~/.bash_profile{Environment.NewLine}" +
                        $"# Add dotnet-sdk tools{Environment.NewLine}" +
                        $"export PATH=\"$PATH:{_packageExecutablePath}\"{Environment.NewLine}" +
                        $"EOF");
                }
            }
        }
    }
}
