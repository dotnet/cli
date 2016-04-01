// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.DotNet.Tools.Run.Tests
{
    public class MuxerTests : TestBase
    {
        private const string KestrelHelloWorldBase = "KestrelHelloWorld";
        private const string KestrelHelloWorldPortable = "KestrelHelloWorldPortable";
        private const string KestrelHelloWorldStandalone = "KestrelHelloWorldStandalone";

        [Fact]
        public void ItRunsKestrelPortableFatAppAfterBuild()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var output = Build(Path.Combine(instance.TestRoot, KestrelHelloWorldPortable));

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var dotnetCommand = new DotnetCommand();

            try
            {
                dotnetCommand.ExecuteAsync($"{output} {args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                dotnetCommand.Kill(true);
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneAppAfterBuild()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var output = Build(Path.Combine(instance.TestRoot, KestrelHelloWorldStandalone));

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var dotnetCommand = new DotnetCommand();

            try
            {
                dotnetCommand.ExecuteAsync($"{output} {args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                dotnetCommand.Kill(true);
            }
        }

        [Fact]
        public void ItRunsKestrelPortableFatAppAfterPublish()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var output = Publish(Path.Combine(instance.TestRoot, KestrelHelloWorldPortable), true);

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var dotnetCommand = new DotnetCommand();

            try
            {
                dotnetCommand.ExecuteAsync($"{output} {args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                dotnetCommand.Kill(true);
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneAppAfterPublish()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelHelloWorldBase)
                                                     .WithLockFiles();

            var output = Publish(Path.Combine(instance.TestRoot, KestrelHelloWorldStandalone), false);

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var command = new TestCommand(output);

            try
            {
                command.ExecuteAsync($"{args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelHelloWorldStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                command.Kill(true);
            }
        }

        private static string Build(string testRoot)
        {
            string appName = Path.GetFileName(testRoot);

            var result = new BuildCommand(
                projectPath: testRoot)
                .ExecuteWithCapturedOutput();

            result.Should().Pass();

            // the correct build assembly is next to its deps.json file 
            var depsJsonFile = Directory.EnumerateFiles(testRoot, appName + FileNameSuffixes.DepsJson, SearchOption.AllDirectories).First();
            return Path.Combine(Path.GetDirectoryName(depsJsonFile), appName + ".dll");
        }

        private static string Publish(string testRoot, bool isPortable)
        {
            string appName = Path.GetFileName(testRoot);

            var publishCmd = new PublishCommand(projectPath: testRoot, output: Path.Combine(testRoot, "bin"));
            var result = publishCmd.ExecuteWithCapturedOutput();
            result.Should().Pass();

            var publishDir = publishCmd.GetOutputDirectory(portable: isPortable).FullName;
            return Path.Combine(publishDir, appName + (isPortable ? ".dll" : FileNameSuffixes.CurrentPlatform.Exe));
        }
    }
}
