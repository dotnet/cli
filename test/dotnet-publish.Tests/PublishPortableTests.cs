﻿using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.DotNet.Tools.Publish.Tests
{
    public class PublishPortableTests : TestBase
    {
        private static readonly IEnumerable<Tuple<string, string>> ExpectedRuntimeOutputs = new[] {
            Tuple.Create("debian-x64", "libuv.so"),
            Tuple.Create("rhel-x64", "libuv.so"),
            Tuple.Create("osx", "libuv.dylib"),
            Tuple.Create("win7-arm", "libuv.dll"),
            Tuple.Create("win7-x86", "libuv.dll"),
            Tuple.Create("win7-x64", "libuv.dll")
        };

        [Fact]
        public void PortableAppWithRuntimeTargetsIsPublishedCorrectly()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("TestAppWithRuntimeTargets")
                .WithLockFiles()
                .WithBuildArtifacts();

            var publishCommand = new PublishCommand(testInstance.TestRoot, forcePortable: true);
            var publishResult = publishCommand.Execute();

            publishResult.Should().Pass();

            var publishDir = publishCommand.GetOutputDirectory();
            publishDir.Should().HaveFiles(new[]
            {
                "TestAppWithRuntimeTargets.dll",
                "TestAppWithRuntimeTargets.deps",
                "TestAppWithRuntimeTargets.deps.json"
            });

            var runtimesOutput = publishDir.Sub("runtimes");

            runtimesOutput.Should().Exist();

            foreach (var output in ExpectedRuntimeOutputs)
            {
                var ridDir = runtimesOutput.Sub(output.Item1);
                ridDir.Should().Exist();

                var nativeDir = ridDir.Sub("native");
                nativeDir.Should().Exist();
                nativeDir.Should().HaveFile(output.Item2);
            }
        }
    }
}
