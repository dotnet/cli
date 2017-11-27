// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    internal class OsxEnvironmentPath : IEnvironmentPath
    {
        private const string PathName = "PATH";
        private readonly string _packageExecutablePathWIthTilde;
        private readonly string _fullPackageExecutablePath;
        private readonly IFile _fileSystem;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly IReporter _reporter;
        private static readonly string PathDDotnetCliToolsPath
            = Environment.GetEnvironmentVariable("DOTNET_CLI_TEST_OSX_PATHSD_PATH")
              ?? @"/etc/paths.d/dotnet-cli-tools";

        public OsxEnvironmentPath(
            string packageExecutablePathWIthTilde,
            string fullPackageExecutablePath,
            IReporter reporter,
            IEnvironmentProvider environmentProvider,
            IFile fileSystem
        )
        {
            _fullPackageExecutablePath = fullPackageExecutablePath ??
                                         throw new ArgumentNullException(nameof(fullPackageExecutablePath));
            _packageExecutablePathWIthTilde = packageExecutablePathWIthTilde ??
                                              throw new ArgumentNullException(nameof(packageExecutablePathWIthTilde));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environmentProvider
                = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _reporter
                = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var script = $"{_packageExecutablePathWIthTilde}";
            _fileSystem.WriteAllText(PathDDotnetCliToolsPath, script);
        }

        private bool PackageExecutablePathExists()
        {
            return _environmentProvider.GetEnvironmentVariable(PathName).Split(':')
                       .Contains(_packageExecutablePathWIthTilde) ||
                   _environmentProvider.GetEnvironmentVariable(PathName).Split(':')
                       .Contains(_fullPackageExecutablePath);
        }

        public void PrintAddPathInstructionIfPathDoesNotExist()
        {
            if (!PackageExecutablePathExists())
            {
                if (_fileSystem.Exists(PathDDotnetCliToolsPath))
                {
                    _reporter.WriteLine(
                        $"You need reopen to be able to run new installed command from shell{Environment.NewLine}" +
                        $"If you are using different a shell that is not sh or bash, you need to ensure {_fullPackageExecutablePath} is in your path");
                }
                else
                {
                    // similar to https://code.visualstudio.com/docs/setup/mac
                    _reporter.WriteLine(
                        $"Cannot find tools executable path in environment PATH. Please ensure {_fullPackageExecutablePath} is added to your PATH.{Environment.NewLine}" +
                        $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                        $"cat << EOF >> ~/.bash_profile{Environment.NewLine}" +
                        $"# Add dotnet-sdk tools{Environment.NewLine}" +
                        $"export PATH=\"$PATH:{_fullPackageExecutablePath}\"{Environment.NewLine}" +
                        $"EOF");
                }
            }
        }
    }
}
