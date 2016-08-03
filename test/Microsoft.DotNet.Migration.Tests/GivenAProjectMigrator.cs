// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAScriptCommandResolver : TestBase
    {
        [Fact]
        public void It_has_a_count_proto()
        {
            var migrator = new ProjectMigrator()

            var testProjectPath = Path.Combine(RepoRoot, "TestAssets", "TestProjects", "PortableTests", "PortableApp");
            var output = Path.Combine(AppContext.BaseDirectory, "out");

            migrator.Migrate(testProjectPath, output).Should().Be(0);
        }
    }
}
