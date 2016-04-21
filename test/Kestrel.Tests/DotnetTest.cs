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

namespace Microsoft.DotNet.Kestrel.Tests
{
    public class DotnetTest : TestBase
    {
        private const string KestrelSampleBase = "KestrelSample";
        private const string KestrelPortable = "KestrelPortable";
        private const string KestrelStandalone = "KestrelStandalone";

        [Fact]
        public void ItRunsKestrelPortableAfterBuild()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelSampleBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            string output = Build(Path.Combine(instance.TestRoot, KestrelPortable));
            var dotnetCommand = new DotnetCommand();

            try
            {
                dotnetCommand.ExecuteAsync($"{output} {args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                dotnetCommand.KillTree();
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneAfterBuild()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelSampleBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var testCommand = new TestCommand(Build(Path.Combine(instance.TestRoot, KestrelStandalone)));

            try
            {
                testCommand.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                testCommand.KillTree();
            }
        }

        [Fact]
        public void ItRunsKestrelPortableAfterPublish()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelSampleBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var dotnetCommand = new DotnetCommand();
            var output = Publish(Path.Combine(instance.TestRoot, KestrelPortable), true);

            try
            {
                dotnetCommand.ExecuteAsync($"{output} {args}");
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                dotnetCommand.KillTree();
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneAfterPublish()
        {
            TestInstance instance = TestAssetsManager.CreateTestInstance(KestrelSampleBase)
                                                     .WithLockFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var command = new TestCommand(Publish(Path.Combine(instance.TestRoot, KestrelStandalone), false));

            try
            {
                command.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                command.KillTree();
            }
        }
        
        private static bool IsRidCompatibleWith(string rid, string otherRid)
        {
            if (rid == otherRid)
            {
                return true;
            }
            
            if (rid.EndsWith("x86") != otherRid.EndsWith("x86"))
            {
                return false;
            }
            
            if (rid.StartsWith("win10-"))
            {
                return otherRid.StartsWith("win8-") || otherRid.StartsWith("win7-");
            }
            
            if (rid.StartsWith("win8-"))
            {
                return otherRid.StartsWith("win7-");
            }
            
            return false;
        }

        private static string Build(string testRoot)
        {
            string appName = Path.GetFileName(testRoot);

            var buildCommand = new BuildCommand(projectPath: testRoot);
            var result = buildCommand.ExecuteWithCapturedOutput();

            result.Should().Pass();

            // the correct build assembly is next to its deps.json file 
            string rid = PlatformServices.Default.Runtime.GetRuntimeIdentifier();
            var depsJsonFiles = Directory.EnumerateFiles(testRoot, appName + FileNameSuffixes.DepsJson, SearchOption.AllDirectories);
            string depsJsonFile;
            if (depsJsonFiles.Count() == 1)
            {
                depsJsonFile = depsJsonFiles.First();
            }
            else
            {
                var standaloneAppDepsJsons = depsJsonFiles.Where((path) => IsRidCompatibleWith(rid, Directory.GetParent(path).Name));
                depsJsonFile = standaloneAppDepsJsons.First();
            }
            
            var appPath = Path.GetDirectoryName(depsJsonFile);
            var exePath = Path.Combine(appPath, buildCommand.GetOutputExecutableName());
            if (File.Exists(exePath))
            {
                // standalone app
                return exePath;
            }
            else
            {
                // portable app
                var dllPath = Path.Combine(appPath, appName + ".dll");
                return dllPath;
            }
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
