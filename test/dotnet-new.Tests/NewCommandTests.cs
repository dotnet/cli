// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.New.Tests
{
    public class NewCommandTests
    {
		private readonly ITestOutputHelper output;

		public NewCommandTests(ITestOutputHelper output)
		{
			this.output = output;
		}

        [Fact]
        public void WhenSwitchIsSkippedThenItPrintsError()
        {
            var cmd = new DotnetCommand().Execute("new Web1.1");

            cmd.ExitCode.Should().NotBe(0);
            
			cmd.StdErr.Should().Be("Unrecognized command or argument 'Web1.1'");
            cmd.StdOut.Should().Be("Specify --help for a list of available options and commands.");
		}
	}
}
