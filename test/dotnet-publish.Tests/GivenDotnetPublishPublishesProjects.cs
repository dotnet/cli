// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.Publish.Tests
{
    public class GivenDotnetPublishPublishesProjects : TestBase
    {
        [Fact]
        public void ItPublishesARunnablePortableApp()
        {
            var testAppName = "MSBuildTestApp";
            var testInstance = TestAssetsManager
                .CreateTestInstance(testAppName);

            var testProjectDirectory = testInstance.TestRoot;

            new RestoreCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            new PublishCommand()
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("--framework netcoreapp1.1")
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";
            var outputDll = Path.Combine(testProjectDirectory, "bin", configuration, "netcoreapp1.1", "publish", $"{testAppName}.dll");

            new TestCommand("dotnet")
                .ExecuteWithCapturedOutput(outputDll)
                .Should().Pass()
                         .And.HaveStdOutContaining("Hello World");
        }

        [RequiresSpecificFrameworkFact("netcoreapp1.0")]
        public void ItPublishesARunnableSelfContainedAppWithAnImplicitRuntime()
        {
            var testAppName = "MSBuildTestApp";

            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var testProjectDirectory = testInstance.Root;

            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();

            new PublishCommand()
                .WithFramework("netcoreapp1.1")
                .WithRuntime(rid)
                .WithWorkingDirectory(testProjectDirectory)
                .Execute("/p:SkipInvalidConfigurations=true")
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputProgram = testProjectDirectory
                .GetDirectory("bin", configuration, "netcoreapp1.1", rid, "publish", $"{testAppName}{Constants.ExeSuffix}")
                .FullName;

            new TestCommand(outputProgram)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                     .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ItPublishesARunnableSelfContainedAppWithAnExplictRuntime()
        {
            var testAppName = "MSBuildTestApp";

            var testInstance = TestAssets.Get(testAppName)
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var testProjectDirectory = testInstance.Root;

            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();

            File.Exists(Path.Combine(AppContext.BaseDirectory, ".runtimeFrameworkVersion")).Should().BeTrue();
            var runtimeFrameworkVersion = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ".runtimeFrameworkVersion"));

            new PublishCommand()
                .WithFramework("netcoreapp1.1")
                .WithRuntime(rid)
                .WithWorkingDirectory(testProjectDirectory)
                .Execute($"/p:SkipInvalidConfigurations=true /p:RuntimeFrameworkVersion={runtimeFrameworkVersion}")
                .Should().Pass();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputProgram = testProjectDirectory
                .GetDirectory("bin", configuration, "netcoreapp1.1", rid, "publish", $"{testAppName}{Constants.ExeSuffix}")
                .FullName;

            new TestCommand(outputProgram)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                     .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ItPublishesAppWhenRestoringToSpecificPackageDirectory()
        {
            var rootPath = TestAssetsManager.CreateTestDirectory().Path;
            var rootDir = new DirectoryInfo(rootPath);

            string dir = "pkgs";
            string args = $"--packages {dir}";

            string newArgs = $"console -f netcoreapp1.1 -o \"{rootPath}\"";
            new NewCommandShim()
                .WithWorkingDirectory(rootPath)
                .Execute(newArgs)
                .Should()
                .Pass();

            new RestoreCommand()
                .WithWorkingDirectory(rootPath)
                .Execute(args)
                .Should()
                .Pass();

            new PublishCommand()
                .WithWorkingDirectory(rootPath)
                .ExecuteWithCapturedOutput()
                .Should().Pass();

            var rid = DotnetLegacyRuntimeIdentifiers.InferLegacyRestoreRuntimeIdentifier();
            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            var outputProgram = rootDir
                .GetDirectory("bin", configuration, "netcoreapp1.1", "publish", $"{rootDir.Name}.dll")
                .FullName;

            new TestCommand(outputProgram)
                .ExecuteWithCapturedOutput()
                .Should().Pass()
                     .And.HaveStdOutContaining("Hello World");
        }
    }
}
