// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Xunit;
using Moq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.PlatformAbstractions;
using System.Threading;
using FluentAssertions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli.Utils.Tests
{
    public class GivenADesktopAppWhichUsesCommandCreateDotnet : TestBase
    {
        [Fact]
        public void It_calls_dotnet_build_on_a_project_successfully()
        {
            var testAssetsManager = GetTestAssetsManager("DesktopTestProjects");
            var testInstance = testAssetsManager
                .CreateTestInstance("DesktopAppWhichCallDotnet")
                .WithLockFiles();
                
            var testProject = Path.Combine(testInstance.TestRoot, "project.json");

            new RunCommand(testProject).Execute().Should().Pass();
        }
    }
}
