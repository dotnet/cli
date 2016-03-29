using Microsoft.DotNet.Tools.Test.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly DirectoryInfo _publishDir;

        public PublishPortableTests()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("PortableTests")
                .WithLockFiles();

            var publishCommand = new PublishCommand(Path.Combine(testInstance.TestRoot, "PortableAppWithNative"));
            var publishResult = publishCommand.Execute();

            publishResult.Should().Pass();

            _publishDir = publishCommand.GetOutputDirectory(portable: true);
        }

        [Fact]
        public void PortableAppWithRuntimeTargetsIsPublishedCorrectly()
        {            
            _publishDir.Should().HaveFiles(new[]
            {
                "PortableAppWithNative.dll",
                "PortableAppWithNative.deps",
                "PortableAppWithNative.deps.json"
            });

            // Prior to `type:platform` trimming, this would have been published.
            _publishDir.Should().NotHaveFile("System.Linq.dll");

            var runtimesOutput = _publishDir.Sub("runtimes");

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

        [Fact]
        public void PortableAppWithIntentionalDowngradePublishesDowngradedManagedCode()
        {
            var testInstance = TestAssetsManager.CreateTestInstance("PortableTests")
                .WithLockFiles();

            var publishCommand = new PublishCommand(Path.Combine(testInstance.TestRoot, "PortableAppWithIntentionalManagedDowngrade"));
            var publishResult = publishCommand.Execute();

            publishResult.Should().Pass();

            var publishDir = publishCommand.GetOutputDirectory(portable: true);
            publishDir.Should().HaveFiles(new[]
            {
                "PortableAppWithIntentionalManagedDowngrade.dll",
                "PortableAppWithIntentionalManagedDowngrade.deps",
                "PortableAppWithIntentionalManagedDowngrade.deps.json",
                "System.Linq.dll"
            });
        }
        
        [Fact]
        public void PortableAppWithRuntimeTargetsDoesNotHaveRuntimeConfigDevJsonFile()
        {
            _publishDir.Should().NotHaveFile("PortableAppWithNative.runtimeconfig.dev.json");
        }
    }
}
