// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.ToolPackage;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Tools.Tests.ComponentMocks
{
    internal class ToolPackageObtainerMock : IToolPackageObtainer
    {
        private readonly Action _beforeRunObtain;
        private static IFileSystem _fileSystem;
        private List<MockFeed> _mockFeeds;

        public ToolPackageObtainerMock(
            IFileSystem fileSystemWrapper = null,
            bool useDefaultFeed = true,
            IEnumerable<MockFeed> additionalFeeds = null,
            Action beforeRunObtain = null)
        {
            _beforeRunObtain = beforeRunObtain ?? (() => { });
            _fileSystem = fileSystemWrapper ?? new FileSystemWrapper();
            _mockFeeds = new List<MockFeed>();

            if (useDefaultFeed)
            {
                _mockFeeds.Add(new MockFeed
                {
                    Type = MockFeedType.FeedFromLookUpNugetConfig,
                    Packages = new List<MockFeedPackage>
                    {
                        new MockFeedPackage
                        {
                            PackageId = "global.tool.console.demo",
                            Version = "1.0.4"
                        }
                    }
                });
            }

            if (additionalFeeds != null)
            {
                _mockFeeds.AddRange(additionalFeeds);
            }
        }

        IObtainTransaction IToolPackageObtainer.CreateObtainTransaction(
            string packageId,
            string packageVersion,
            FilePath? nugetconfig,
            string targetframework, string source)
        {
            return new ObtainTransactionMock(
                packageId,
                packageVersion,
                nugetconfig,
                targetframework,
                source,
                _beforeRunObtain,
                _fileSystem,
                _mockFeeds);
        }
    }
}
