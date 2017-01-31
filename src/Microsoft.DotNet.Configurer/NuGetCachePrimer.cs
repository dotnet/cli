﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Configurer
{
    public class NuGetCachePrimer : INuGetCachePrimer
    {
        private static IReadOnlyList<IReadOnlyList<string>> _templatesUsedToPrimeCache = new List<IReadOnlyList<string>>()
        {
            new List<string>() { "mvc", "-f", "1.0", "-au", "Individual", "--debug:ephemeral-hive" },
            new List<string>() { "mvc", "-f", "1.1", "-au", "Individual", "--debug:ephemeral-hive" }
        };

        private readonly ICommandFactory _commandFactory;

        private readonly IDirectory _directory;

        private readonly IFile _file;

        private readonly INuGetPackagesArchiver _nugetPackagesArchiver;

        private readonly INuGetCacheSentinel _nuGetCacheSentinel;

        public NuGetCachePrimer(
            ICommandFactory commandFactory,
            INuGetPackagesArchiver nugetPackagesArchiver,
            INuGetCacheSentinel nuGetCacheSentinel)
            : this(commandFactory,
                nugetPackagesArchiver,
                nuGetCacheSentinel,
                FileSystemWrapper.Default.Directory,
                FileSystemWrapper.Default.File)
        {
        }

        internal NuGetCachePrimer(
            ICommandFactory commandFactory,
            INuGetPackagesArchiver nugetPackagesArchiver,
            INuGetCacheSentinel nuGetCacheSentinel,
            IDirectory directory,
            IFile file)
        {
            _commandFactory = commandFactory;

            _directory = directory;

            _nugetPackagesArchiver = nugetPackagesArchiver;

            _nuGetCacheSentinel = nuGetCacheSentinel;

            _file = file;
        }

        public void PrimeCache()
        {
            if (SkipPrimingTheCache())
            {
                return;
            }

            var extractedPackagesArchiveDirectory = _nugetPackagesArchiver.ExtractArchive();

            PrimeCacheUsingArchive(extractedPackagesArchiveDirectory);
        }

        private bool SkipPrimingTheCache()
        {
            return !_file.Exists(_nugetPackagesArchiver.NuGetPackagesArchive);
        }

        private void PrimeCacheUsingArchive(string extractedPackagesArchiveDirectory)
        {
            bool succeeded = true;

            foreach (IReadOnlyList<string> templateInfo in _templatesUsedToPrimeCache)
            {
                if (succeeded)
                {
                    using (var temporaryDotnetNewDirectory = _directory.CreateTemporaryDirectory())
                    {
                        var workingDirectory = temporaryDotnetNewDirectory.DirectoryPath;

                        succeeded &= CreateTemporaryProject(workingDirectory, templateInfo);

                        if (succeeded)
                        {
                            succeeded &= RestoreTemporaryProject(extractedPackagesArchiveDirectory, workingDirectory);
                        }
                    }
                }
            }

            if (succeeded)
            {
                _nuGetCacheSentinel.CreateIfNotExists();
            }
        }

        private bool CreateTemporaryProject(string workingDirectory, IReadOnlyList<string> templateInfo)
        {
            return RunCommand(
                "new",
                templateInfo,
                workingDirectory);
        }

        private bool RestoreTemporaryProject(string extractedPackagesArchiveDirectory, string workingDirectory)
        {
            return RunCommand(
                "restore",
                new[] { "-s", extractedPackagesArchiveDirectory },
                workingDirectory);
        }

        private bool RunCommand(string commandToExecute, IEnumerable<string> args, string workingDirectory)
        {
            var command = _commandFactory
                .Create(commandToExecute, args)
                .WorkingDirectory(workingDirectory)
                .CaptureStdOut()
                .CaptureStdErr();

            var commandResult = command.Execute();

            if (commandResult.ExitCode != 0)
            {
                Reporter.Verbose.WriteLine(commandResult.StdErr);

                Reporter.Error.WriteLine(
                    string.Format(LocalizableStrings.FailedToPrimeCacheError, commandToExecute, commandResult.ExitCode));
            }

            return commandResult.ExitCode == 0;
        }
    }
}
