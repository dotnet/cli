// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.Tools.Install.Tool;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ToolPackageFactory : IToolPackageFactory
    {
        private const string NameOfNestedPackageDirectory = ".pkg";

        public (IToolPackageStore, IToolPackageInstaller) CreateToolPackageStoreAndInstaller(
            DirectoryPath? nonGlobalLocation = null)
        {
            var toolPackageStore =
                new ToolPackageStore(GetPackageLocation(nonGlobalLocation));
            var toolPackageInstaller = new ToolPackageInstaller(
                toolPackageStore,
                new ProjectRestorer(Reporter.Output));

            return (toolPackageStore, toolPackageInstaller);
        }

        private static DirectoryPath GetPackageLocation(DirectoryPath? nonGlobalLocation)
        {
            DirectoryPath packageLocation;

            if (nonGlobalLocation.HasValue)
            {
                packageLocation = nonGlobalLocation.Value.WithSubDirectories(NameOfNestedPackageDirectory);
            }
            else
            {
                var cliFolderPathCalculator = new CliFolderPathCalculator();
                packageLocation = new DirectoryPath(cliFolderPathCalculator.ToolsPackagePath);
            }

            return packageLocation;
        }
    }
}
