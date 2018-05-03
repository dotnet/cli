// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.Utils;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tests
{
    public class RootEnvironmentTests
    {
        [Theory]
        [InlineData(true, "DOTNET_ROOT")]
        [InlineData(false, "DOTNET_ROOT(x86)")]
        public void ItSetsDotnetRootToMuxerPath(bool is64Bit, string varName)
        {
            var expectedPath = Path.GetDirectoryName(new Muxer().MuxerPath);
            var mockEnv = new Mock<IEnvironmentProvider>();
            mockEnv.SetupGet(e => e.Is64BitProcess).Returns(is64Bit);
            mockEnv.Setup(e => e.SetEnvironmentVariable(varName, expectedPath, EnvironmentVariableTarget.Process));

            Program.InitializeEnvironment(mockEnv.Object);

            mockEnv.VerifyAll();
        }
    }
}
