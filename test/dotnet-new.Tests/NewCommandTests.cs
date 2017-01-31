// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.New.Tests
{
    public class NewCommandTests
    {
        [Fact]
        public void WhenSwitchIsSkippedThenItPrintsError()
        {
            var cmd = new DotnetCommand().Execute("new Web1.1");

            cmd.ExitCode.Should().NotBe(0);
            
			cmd.StdErr.Should().Be("No templates matched the input template name: [Web1.1]\n");
		}
	}
}
