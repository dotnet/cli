// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Publish.Tests
{
    public class GivenDotnetPublishPublisheswithCacheProjects : TestBase
    {
        private static string _tfm = "netcoreapp2.0";
        private static string _frameworkVersion = Microsoft.DotNet.TestFramework.TestAssetInstance.CurrentRuntimeFrameworkVersion;
        private static string _arch = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.RuntimeArchitecture.ToLowerInvariant();
        [Fact]
        public void ItPublishesARunnablePortableApp()
        {
            var testAppName = "NewtonSoftDependentProject";
            var profileProjectName = "NewtonsoftProfile";

            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .UseCurrentRuntimeFrameworkVersion();

            var testProjectDirectory = testInstance.Root.ToString();
            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();
            var localAssemblyCache = Path.Combine(testProjectDirectory, "localAssemblyCache");
            var intermediateWorkingDirectory = Path.Combine(testProjectDirectory, "workingDirectory");
            var profileProjectPath = TestAssets.Get(profileProjectName).Root.FullName;
            var profileProject = Path.Combine(profileProjectPath, $"{profileProjectName}.xml");

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new CacheCommand()
                .WithEntries(profileProject)
                .WithFramework(_tfm)
                .WithRuntime(rid)
                .WithOutput(localAssemblyCache)
                .WithRuntimeFrameworkVersion(_frameworkVersion)
                .WithIntermediateWorkingDirectory(intermediateWorkingDirectory)
                .Execute($"--preserve-working-dir")
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";
            var profileFilter = Path.Combine(localAssemblyCache, _arch, _tfm, "artifact.xml");

            new PublishCommand()
                .WithFramework(_tfm)
                .WithWorkingDirectory(testProjectDirectory)
                .WithProFileProject(profileFilter)
                .Execute()
                .Should().Pass();

            var outputDll = Path.Combine(testProjectDirectory, "bin", configuration, _tfm, "publish", $"{testAppName}.dll");

            new TestCommand("dotnet")
                .WithEnvironmentVariable("DOTNET_SHARED_PACKAGES", localAssemblyCache)
                .ExecuteWithCapturedOutput(outputDll)
                .Should().Pass()
                .And.HaveStdOutContaining("{}");
        }

        [Fact]
        public void AppFailsDueToMissingCache()
        {
            var testAppName = "NewtonSoftDependentProject";
            var profileProjectName = "NewtonsoftProfile";

            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .UseCurrentRuntimeFrameworkVersion();

            var testProjectDirectory = testInstance.Root.ToString();
            var profileProjectPath = TestAssets.Get(profileProjectName).Root.FullName;
            var profileFilter = Path.Combine(profileProjectPath, "NewtonsoftFilterProfile.xml");

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            new PublishCommand()
                .WithFramework(_tfm)
                .WithWorkingDirectory(testProjectDirectory)
                .WithProFileProject(profileFilter)
                .Execute()
                .Should().Pass();

            var outputDll = Path.Combine(testProjectDirectory, "bin", configuration, _tfm, "publish", $"{testAppName}.dll");

            new TestCommand("dotnet")
                .ExecuteWithCapturedOutput(outputDll)
                .Should().Fail()
                .And.HaveStdErrContaining("assembly specified in the dependencies manifest was not found -- package: 'newtonsoft.json',");
        }

        [Fact]
        public void ItPublishesAnAppWithMultipleProfiles()
        {
            var testAppName = "MultiDependentProject";
            var profileProjectName = "NewtonsoftProfile";
            var profileProjectName1 = "FluentProfile";

            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .UseCurrentRuntimeFrameworkVersion();

            var testProjectDirectory = testInstance.Root.ToString();
            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();
            var localAssemblyCache = Path.Combine(testProjectDirectory, "lAC");
            var intermediateWorkingDirectory = Path.Combine(testProjectDirectory, "workingDirectory");

            var profileProjectPath = TestAssets.Get(profileProjectName).Root.FullName;
            var profileProject = Path.Combine(profileProjectPath, $"{profileProjectName}.xml");
            var profileFilter = Path.Combine(profileProjectPath, "NewtonsoftFilterProfile.xml");

            var profileProjectPath1 = TestAssets.Get(profileProjectName1).Root.FullName; 
            var profileProject1 = Path.Combine(profileProjectPath1, $"{profileProjectName1}.xml");
            var profileFilter1 = Path.Combine(profileProjectPath1, "FluentFilterProfile.xml");

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute()
                .Should().Pass();

            new CacheCommand()
                .WithEntries(profileProject)
                .WithEntries(profileProject1)
                .WithFramework(_tfm)
                .WithRuntime(rid)
                .WithOutput(localAssemblyCache)
                .WithRuntimeFrameworkVersion(_frameworkVersion)
                .WithIntermediateWorkingDirectory(intermediateWorkingDirectory)
                .Execute($"--preserve-working-dir")
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            new PublishCommand()
                .WithFramework(_tfm)
                .WithWorkingDirectory(testProjectDirectory)
                .WithProFileProject(profileFilter)
                .WithProFileProject(profileFilter1)
                .Execute()
                .Should().Pass();

            var outputDll = Path.Combine(testProjectDirectory, "bin", configuration, _tfm, "publish", $"{testAppName}.dll");

            new TestCommand("dotnet")
                .WithEnvironmentVariable("DOTNET_SHARED_PACKAGES", localAssemblyCache)
                .ExecuteWithCapturedOutput(outputDll)
                .Should().Pass()
                .And.HaveStdOutContaining("{}");
        }
    }
}
