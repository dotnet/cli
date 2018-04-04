// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenAFrameworkDependencyFile
    {
        [WindowsOnlyFact]
        public void WhenPassSeveralCompatibleRuntimeIdentifiersItOutMostFitRid()
        {
            var frameworkDependencyFile = new FrameworkDependencyFile();
            frameworkDependencyFile.TryGetMostFitRuntimeIdentifier(new string[] { "win", "any"}, out string mostFitRid).Should().BeTrue();
            mostFitRid.Should().Be("win");
        }

        [WindowsOnlyFact]
        public void WhenPassSeveralCompatibleRuntimeIdentifiersWithDuplicationItOutMostFitRid()
        {
            var frameworkDependencyFile = new FrameworkDependencyFile();
            frameworkDependencyFile.TryGetMostFitRuntimeIdentifier(new string[] { "win", "win", "any" }, out string mostFitRid).Should().BeTrue();
            mostFitRid.Should().Be("win");
        }

        [WindowsOnlyFact]
        public void WhenPassSeveralNonCompatibleRuntimeIdentifiersItReturnsFalse()
        {
            var frameworkDependencyFile = new FrameworkDependencyFile();
            frameworkDependencyFile.TryGetMostFitRuntimeIdentifier(new string[] { "centos", "debian" }, out var _).Should().BeFalse();
        }
    }
}
