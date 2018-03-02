// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tests.Commands
{
    internal class PassThroughToolPackageFactory : IToolPackageFactory
    {
        private readonly IToolPackageStore _toolPackageStore;
        private readonly IToolPackageInstaller _toolPackageInstaller;

        public PassThroughToolPackageFactory(IToolPackageStore toolPackageStore, 
            IToolPackageInstaller toolPackageInstaller)
        {
            _toolPackageStore = toolPackageStore;
            _toolPackageInstaller = toolPackageInstaller;
        }

        public (IToolPackageStore, IToolPackageInstaller) 
            CreateToolPackageStoreAndInstaller(DirectoryPath? nonGlobalLocation = null)
        {
            return (_toolPackageStore, _toolPackageInstaller);
        }
    }
}
