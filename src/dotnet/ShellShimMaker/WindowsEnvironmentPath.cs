// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShimMaker
{
    internal class WindowsEnvironmentPath : IEnvironmentPath
    {
        private readonly IReporter _reporter;
        private readonly IEnvironmentProvider _environmentProvider;
        private const string PathName = "PATH";
        private readonly string _packageExecutablePath;

        public WindowsEnvironmentPath(
            string packageExecutablePath, IReporter reporter,
            IEnvironmentProvider environmentProvider)
        {
            _packageExecutablePath 
                = packageExecutablePath ?? throw new ArgumentNullException(nameof(packageExecutablePath));
            _environmentProvider
                = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
            _reporter
                = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public void AddPackageExecutablePathToUserPath()
        {
            if (PackageExecutablePathExists()) return;

            var existingUserEnvPath = Environment.GetEnvironmentVariable(PathName, EnvironmentVariableTarget.User);

            Environment.SetEnvironmentVariable(
                PathName,
                $"{existingUserEnvPath};{_packageExecutablePath}",
                EnvironmentVariableTarget.User);
        }

        private bool PackageExecutablePathExists()
        {
            return _environmentProvider.GetEnvironmentVariable(PathName).Split(';').Contains(_packageExecutablePath);
        }

        public void PrintAddPathInstructionIfPathDoesNotExist()
        {
            if (!PackageExecutablePathExists())
            {
                if (Environment.GetEnvironmentVariable(PathName, EnvironmentVariableTarget.User).Split(';').Contains(_packageExecutablePath))
                {
                    _reporter.WriteLine(
                        $"You need reopen shell to be able to run new installed command.");
                }
                else
                {
                    _reporter.WriteLine(
                        $"Cannot find tools executable path in environement PATH. Please ensure {_packageExecutablePath} is added to your PATH.{Environment.NewLine}" +
                        $"If you are using bash, you can add it by running following command:{Environment.NewLine}{Environment.NewLine}" +
                        $"setx PATH \\\"%PATH%;{_packageExecutablePath}\"{Environment.NewLine}");
                }
            }
        }
    }
}
