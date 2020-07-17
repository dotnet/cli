// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Tools.Common;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Utils
{
    public class DotDefaultPathCorrectorTests
    {
        [Fact]
        public void ItCanCorrectDotDefaultPath()
        {
            var existingPath = @"C:\Users\myname\AppData\Local\Microsoft\WindowsApps;C:\Users\myname\.dotnet\tools";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string correctPath).Should().BeTrue();
            correctPath.Should().Be(@"%USERPROFILE%\AppData\Local\Microsoft\WindowsApps");
        }

        [Fact]
        public void ItCanTellNoCorrectionNeeded()
        {
            var existingPath = @"C:\Users\myname\AppData\Local\Microsoft\WindowsApps;";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string _).Should().BeFalse();
        }

        [Fact]
        public void GivenEmptyItCanTellNoCorrectionNeeded()
        {
            var existingPath = "";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string _).Should().BeFalse();
        }

        [Fact]
        public void GivenSubsequencePathItCanCorrectDotDefaultPath()
        {
            var existingPath =
                @"C:\Users\myname\AppData\Local\Microsoft\WindowsApps;C:\Users\myname\.dotnet\tools;C:\Users\myname\other;";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string correctPath).Should().BeTrue();
            correctPath.Should().Be(@"%USERPROFILE%\AppData\Local\Microsoft\WindowsApps;%USERPROFILE%\other");
        }

        [Fact]
        public void GivenSubsequencePathWithExtraFormatItCanCorrectDotDefaultPath()
        {
            var existingPath =
                @";C:\Users\myname\AppData\Local\Microsoft\WindowsApps;C:\Users\myname\.dotnet\tools;C:\Users\myname\other;;";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string correctPath).Should().BeTrue();
            correctPath.Should().Be(@"%USERPROFILE%\AppData\Local\Microsoft\WindowsApps;%USERPROFILE%\other");
        }

        [Fact]
        public void GivenNoToolPathItCanTellNoCorrectionNeeded()
        {
            var existingPath = @"C:\Users\myname\AppData\Local\Microsoft\WindowsApps;C:\Users\myname\other";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string _).Should().BeFalse();
        }

        [Fact]
        public void Given2InstallationItCanCorrectDotDefaultPath()
        {
            var existingPath = @"C:\Users\user1\AppData\Local\Microsoft\WindowsApps;C:\Users\user2\AppData\otherapp;C:\Users\user1\.dotnet\tools;C:\Users\user2\.dotnet\tools";
            DotDefaultPathCorrector.NeedCorrection(existingPath, out string correctPath).Should().BeTrue();
            correctPath.Should().Be(@"%USERPROFILE%\AppData\Local\Microsoft\WindowsApps;%USERPROFILE%\AppData\otherapp");
        }
    }
}
