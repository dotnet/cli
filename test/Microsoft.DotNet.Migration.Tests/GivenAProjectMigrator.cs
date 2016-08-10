// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using Microsoft.DotNet.ProjectJsonMigration;
using System;
using System.IO;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenAProjectMigrator : TestBase
    {
        [Fact]
        public void It_passes_a_smoke_test()
        {
            var migrator = new ProjectMigrator();

            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "PortableTests", "PortableApp");
            var output = Path.Combine(AppContext.BaseDirectory, "out");

            migrator.Migrate(new MigrationSettings(testProjectPath, output)).Should().Be(0);
        }
    }
}
