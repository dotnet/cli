// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Tools.Builder.Tests
{
    public class GivenThatDotnetBuildBuildsForOtherRids : TestBase
    {

        private static readonly dynamic[] BuildOutputForRidData = new[]
        {
            new 
            { 
                Rid="centos.7-x64",
                HostExtension="", 
                ExpectedArtifacts=new string[] { "libhostfxr.so", "libhostpolicy.so" } 
            }, 
            new 
            { 
                Rid="rhel.7.2-x64",
                HostExtension="", 
                ExpectedArtifacts=new string[] { "libhostfxr.so", "libhostpolicy.so" } 
            }, 
            new 
            { 
                Rid="ubuntu.14.04-x64",
                HostExtension="", 
                ExpectedArtifacts=new string[] { "libhostfxr.so", "libhostpolicy.so" } 
            }, 
            new 
            { 
                Rid="win7-x64",
                HostExtension=".exe", 
                ExpectedArtifacts=new string[] { "hostfxr.dll", "hostpolicy.dll" } 
            }, 
            new 
            { 
                Rid="osx.10.11-x64",
                HostExtension="", 
                ExpectedArtifacts=new string[] { "libhostfxr.dylib", "libhostpolicy.dylib" } 
            }
        };

        [Fact]
        public void It_has_expected_outputs_for_all_rids()
        {
            var instance = TestAssetsManager.CreateTestInstance(Path.Combine("PortableTests"))
                .WithLockFiles();
                
            var testProject = Path.Combine(instance.TestRoot, "StandaloneApp", "project.json");

            var buildCommand = new BuildCommand(testProject);
            buildCommand.WorkingDirectory = Path.GetDirectoryName(testProject);
            buildCommand.Execute().Should().Pass();

            foreach (var buildOutputData in BuildOutputForRidData)
            {
                DirectoryInfo builtDir = buildCommand.GetOutputDirectory(portable: false, runtime: buildOutputData.Rid);

                builtDir.Should().HaveFile("StandaloneApp"+ buildOutputData.HostExtension);

                foreach (var artifact in buildOutputData.ExpectedArtifacts)
                {
                     builtDir.Should().HaveFile(artifact);
                }
            }
        }
    }
}
