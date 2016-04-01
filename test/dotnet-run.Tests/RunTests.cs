// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.DotNet.Tools.Run.Tests
{
    public class RunTests : TestBase
    {
        private const string PortableAppsTestBase = "PortableTests";
        private const string RunTestsBase = "RunTestsApps";
        private const string KestrelHelloWorldBase = "KestrelHelloWorld";
        private const string KestrelHelloWorldPortable = "KestrelHelloWorldPortable";
        private const string KestrelHelloWorldStandalone = "KestrelHelloWorldStandalone";


        [WindowsOnlyFact]
        public void RunsSingleTarget()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(RunTestsBase, "TestAppFullClr"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).ExecuteWithCapturedOutput().Should().Pass().And.HaveStdOutContaining("NET451, ARGS: 0");
        }

        [Fact]
        public void RunsDefaultWhenPresent()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(RunTestsBase, "TestAppMultiTarget"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).Execute().Should().Pass();
        }

        [Fact]
        public void FailsWithMultipleTargetAndNoDefault()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(RunTestsBase, "TestAppMultiTargetNoCoreClr"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).Execute().Should().Fail();
        }

        [Fact]
        public void ItRunsPortableApps()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(PortableAppsTestBase, "PortableApp"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).Execute().Should().Pass();
        }

        [Fact(Skip = "https://github.com/dotnet/cli/issues/1940")]
        public void ItRunsPortableAppsWithNative()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(PortableAppsTestBase, "PortableAppWithNative"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).Execute().Should().Pass();
        }

        [Fact]
        public void ItRunsStandaloneApps()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(Path.Combine(PortableAppsTestBase, "StandaloneApp"))
                                                     .WithLockFiles()
                                                     .WithBuildArtifacts();
            new RunCommand(instance.TestRoot).Execute().Should().Pass();
        }

        [Fact]
        public void ItRunsKestrelPortableFatApp()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var runCommand = new RunCommand(Path.Combine(instance.TestRoot, KestrelHelloWorldPortable));

            try
            {
                runCommand.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                runCommand.KillTree();
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneApp()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var runCommand = new RunCommand(Path.Combine(instance.TestRoot, KestrelHelloWorldStandalone));

            try
            {
                runCommand.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                runCommand.KillTree();
            }
        }

        private void CopyProjectToTempDir(string projectDir, TempDirectory tempDir)
        {
            // copy all the files to temp dir
            foreach (var file in Directory.EnumerateFiles(projectDir))
            {
                tempDir.CopyFile(file);
            }
        }

        private string GetProjectPath(TempDirectory projectDir)
        {
            return Path.Combine(projectDir.Path, "project.json");
        }
    }
}
