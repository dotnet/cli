// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.List.PackageReferences
{
    internal class ListPackageReferencesCommand : CommandBase
    {
        private readonly string _fileOrDirectory;

        public ListPackageReferencesCommand(
            AppliedOption appliedCommand,
            ParseResult parseResult) : base(parseResult)
        {
            if (appliedCommand == null)
            {
                throw new ArgumentNullException(nameof(appliedCommand));
            }

            _fileOrDirectory = appliedCommand.Arguments.Single();
        }

        public override int Execute()
        {
            var msbuildProj = MsbuildProject.FromFileOrDirectory(new ProjectCollection(), _fileOrDirectory);

            var packages = msbuildProj.GetPackageReferences();
            if (!packages.Any())
            {
                Reporter.Output.WriteLine(string.Format(
                                              CommonLocalizableStrings.NoReferencesFound,
                                              CommonLocalizableStrings.Package,
                                              _fileOrDirectory));
                return 0;
            }

            Reporter.Output.WriteLine($"{CommonLocalizableStrings.PackageReferenceOneOrMore}");
            Reporter.Output.WriteLine(new string('-', CommonLocalizableStrings.PackageReferenceOneOrMore.Length));
            foreach (var package in packages)
            {
                Reporter.Output.WriteLine(package.Include);
            }

            return 0;
        }
    }
}
