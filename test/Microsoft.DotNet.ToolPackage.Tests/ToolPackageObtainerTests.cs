// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Install.Tool;
using Xunit;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using System.Net;
using System.Transactions;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.ToolPackage.Tests
{
    public class ToolPackageObtainerTests : TestBase
    {

        [Fact]
        public void GivenNoFeedItThrows()
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            ToolPackageObtainer packageObtainer =
                new ToolPackageObtainer(
                new DirectoryPath(toolsPath),
                new DirectoryPath("no such path"),
                GetUniqueTempProjectPathEachTest,
                new Lazy<string>(),
                new ProjectRestorer());

            Action a = () => packageObtainer.CreateObtainTransaction(
                packageId: TestPackageId,
                packageVersion: TestPackageVersion,
                targetframework: _testTargetframework).ObtainAndReturnExecutablePath();

            a.ShouldThrow<PackageObtainException>();
        }

        [Fact]
        public void GivenOfflineFeedWhenCallItCanDownloadThePackage()
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            ToolPackageObtainer packageObtainer =
                new ToolPackageObtainer(
                    toolsPath: new DirectoryPath(toolsPath),
                    offlineFeedPath: new DirectoryPath(GetTestLocalFeedPath()),
                    getTempProjectPath: GetUniqueTempProjectPathEachTest,
                    bundledTargetFrameworkMoniker: new Lazy<string>(),
                    projectRestorer: new ProjectRestorer());

            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework);

            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            var executable = toolConfigurationAndExecutablePath
                .Executable;

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            executable.Value.Should().NotContain(GetTestLocalFeedPath(), "Executable should not be still in fallbackfolder");
            executable.Value.Should().Contain(toolsPath, "Executable should be copied to tools Path");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCanDownloadThePackage(
            bool testMockBehaviorIsInSync)
        {
            FilePath nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync, nugetConfigPath.Value);

            var obtainTransaction
                = packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);

            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            FilePath executable = toolConfigurationAndExecutablePath.Executable;
            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNugetConfigAndPackageNameAndVersionAndTargetFrameworkWhenCallItCreateAssetFile(
            bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync, nugetConfigPath.Value);

            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);

            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            /*
              From mytool.dll to project.assets.json
               .dotnet/.tools/packageid/version/packageid/version/mytool.dll
                      /dependency1 package id/
                      /dependency2 package id/
                      /project.assets.json
             */
            var assetJsonPath = toolConfigurationAndExecutablePath
                .Executable
                .GetDirectoryPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .GetParentPath()
                .WithFile("project.assets.json").Value;

            File.Exists(assetJsonPath)
                .Should()
                .BeTrue(assetJsonPath + " should be created");

            File.Delete(assetJsonPath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoNugetConfigFilePathItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var uniqueTempProjectPath = GetUniqueTempProjectPathEachTest();
            var tempProjectDirectory = uniqueTempProjectPath.GetDirectoryPath();
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempProjectDirectory.Value);

            /*
             * In test, we don't want NuGet to keep look up, so we point current directory to nugetconfig.
             */

            Directory.SetCurrentDirectory(nugetConfigPath.GetDirectoryPath().Value);

            IToolPackageObtainer packageObtainer;
            if (testMockBehaviorIsInSync)
            {
                packageObtainer = new ToolPackageObtainerMock(toolsPath: toolsPath);
            }
            else
            {
                packageObtainer = new ToolPackageObtainer(
                    new DirectoryPath(toolsPath),
                    new DirectoryPath("no such path"),
                    () => uniqueTempProjectPath,
                    new Lazy<string>(),
                    new ProjectRestorer());
            }

            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework);
            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            var executable = toolConfigurationAndExecutablePath.Executable;

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoPackageVersionItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync, nugetConfigPath.Value);

            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    nugetconfig: nugetConfigPath,
                    targetframework: _testTargetframework);
            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            var executable = toolConfigurationAndExecutablePath.Executable;

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAllButNoTargetFrameworkItCanDownloadThePackage(bool testMockBehaviorIsInSync)
        {
            var nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            IToolPackageObtainer packageObtainer;
            if (testMockBehaviorIsInSync)
            {
                packageObtainer = new ToolPackageObtainerMock(additionalFeeds:
                    new List<MockFeed>
                    {
                        new MockFeed
                        {
                            Type = MockFeedType.ExplicitNugetConfig,
                            Uri = nugetConfigPath.Value,
                            Packages = new List<MockFeedPackage>
                            {
                                new MockFeedPackage
                                {
                                    PackageId = "global.tool.console.demo",
                                    Version = "1.0.4"
                                }
                            }
                        }
                    }, 
                    toolsPath: toolsPath);
            }
            else
            {
                packageObtainer = new ToolPackageObtainer(
                    new DirectoryPath(toolsPath),
                    new DirectoryPath("no such path"),
                    GetUniqueTempProjectPathEachTest,
                    new Lazy<string>(() => BundledTargetFramework.GetTargetFrameworkMoniker()),
                    new ProjectRestorer());
            }
            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nugetConfigPath);
            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            var executable = toolConfigurationAndExecutablePath.Executable;

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenNonExistentNugetConfigFileItThrows(bool testMockBehaviorIsInSync)
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer =
                ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            var nonExistNugetConfigFile = new FilePath("NonExistent.file");
            Action a = () =>
            {
                var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    nugetconfig: nonExistNugetConfigFile,
                    targetframework: _testTargetframework);
                var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);
            };

            a.ShouldThrow<PackageObtainException>()
                .And
                .Message.Should().Contain(string.Format(
                    CommonLocalizableStrings.NuGetConfigurationFileDoesNotExist,
                    Path.GetFullPath(nonExistNugetConfigFile.Value)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenASourceItCanObtainThePackageFromThatSource(bool testMockBehaviorIsInSync)
        {
            DownloadPlatformsPackage();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer = ConstructDefaultPackageObtainer(
                toolsPath,
                testMockBehaviorIsInSync,
                addSourceFeedWithFilePath: GetTestLocalFeedPath());
            var obtainTransaction =
                packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework,
                    source: GetTestLocalFeedPath());

            var toolConfigurationAndExecutablePath = RunInTransaction(obtainTransaction);

            var executable = toolConfigurationAndExecutablePath.Executable;

            File.Exists(executable.Value)
                .Should()
                .BeTrue(executable + " should have the executable");

            File.Delete(executable.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenFailedRestoreItCanRollBack(bool testMockBehaviorIsInSync)
        {
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer = ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            var obtainAndReturnExecutablePathtransactional = packageObtainer.CreateObtainTransaction(
                    packageId: "non exist package id",
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework);

            try
            {
                using (var t = new TransactionScope())
                {
                    Transaction.Current.EnlistVolatile(obtainAndReturnExecutablePathtransactional, EnlistmentOptions.None);
                    obtainAndReturnExecutablePathtransactional.ObtainAndReturnExecutablePath();
                    t.Complete();
                }
            }
            catch (PackageObtainException)
            {
                // catch the intent error
            }

            AssertRollBack(toolsPath);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GiveSucessRestoreButFailedOnNextStepItCanRollBack(bool testMockBehaviorIsInSync)
        {
            FilePath nugetConfigPath = WriteNugetConfigFileToPointToTheFeed();
            var toolsPath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());

            var packageObtainer = ConstructDefaultPackageObtainer(toolsPath, testMockBehaviorIsInSync);

            var obtainAndReturnExecutablePathtransactional = packageObtainer.CreateObtainTransaction(
                    packageId: TestPackageId,
                    packageVersion: TestPackageVersion,
                    targetframework: _testTargetframework);

            void FailedStepAfterSuccessRestore() => throw new GracefulException("simulated error");

            try
            {
                using (var t = new TransactionScope())
                {
                    Transaction.Current.EnlistVolatile(obtainAndReturnExecutablePathtransactional, EnlistmentOptions.None);
                    obtainAndReturnExecutablePathtransactional.ObtainAndReturnExecutablePath();

                    FailedStepAfterSuccessRestore();
                    t.Complete();
                }
            }
            catch (GracefulException)
            {
                // catch the simulated error
            }

            AssertRollBack(toolsPath);
        }

        private static void AssertRollBack(string toolsPath)
        {
            if (!Directory.Exists(toolsPath))
            {
                return; // nothing at all
            }

            Directory.GetFiles(toolsPath).Should().BeEmpty();
            Directory.GetDirectories(toolsPath)
                .Should().NotContain(d => !new DirectoryInfo(d).Name.Equals(".stage"),
                "no broken folder, exclude stage folder");

            Directory.GetDirectories(Path.Combine(toolsPath, ".stage"))
                .Should().BeEmpty("nothing in stage folder");
        }

        private static readonly Func<FilePath> GetUniqueTempProjectPathEachTest = () =>
        {
            var tempProjectDirectory =
                new DirectoryPath(Path.GetTempPath()).WithSubDirectories(Path.GetRandomFileName());
            var tempProjectPath =
                tempProjectDirectory.WithFile(Path.GetRandomFileName() + ".csproj");
            return tempProjectPath;
        };

        private static IToolPackageObtainer ConstructDefaultPackageObtainer(
            string toolsPath,
            bool testMockBehaviorIsInSync = false,
            string addNugetConfigFeedWithFilePath = null,
            string addSourceFeedWithFilePath = null)
        {
            if (testMockBehaviorIsInSync)
            {
                if (addNugetConfigFeedWithFilePath != null)
                {
                    return new ToolPackageObtainerMock(additionalFeeds:
                        new List<MockFeed>
                        {
                            new MockFeed
                            {
                                Type = MockFeedType.ExplicitNugetConfig,
                                Uri = addNugetConfigFeedWithFilePath,
                                Packages = new List<MockFeedPackage>
                                {
                                    new MockFeedPackage
                                    {
                                        PackageId = "global.tool.console.demo",
                                        Version = "1.0.4"
                                    }
                                }
                            }
                        }, toolsPath: toolsPath);
                }

                if (addSourceFeedWithFilePath != null)
                {
                    return new ToolPackageObtainerMock(additionalFeeds:
                        new List<MockFeed>
                        {
                            new MockFeed
                            {
                                Type = MockFeedType.Source,
                                Uri = addSourceFeedWithFilePath,
                                Packages = new List<MockFeedPackage>
                                {
                                    new MockFeedPackage
                                    {
                                        PackageId = "global.tool.console.demo",
                                        Version = "1.0.4"
                                    }
                                }
                            }
                        },
                        toolsPath: toolsPath);
                }

                return new ToolPackageObtainerMock(toolsPath: toolsPath);
            }

            return new ToolPackageObtainer(
                new DirectoryPath(toolsPath),
                new DirectoryPath("no such path"),
                GetUniqueTempProjectPathEachTest,
                new Lazy<string>(),
                new ProjectRestorer());
        }

        private static FilePath WriteNugetConfigFileToPointToTheFeed()
        {
            var nugetConfigName = "nuget.config";

            var tempPathForNugetConfigWithWhiteSpace =
                Path.Combine(Path.GetTempPath(),
                    Path.GetRandomFileName() + " " + Path.GetRandomFileName());
            Directory.CreateDirectory(tempPathForNugetConfigWithWhiteSpace);

            NuGetConfig.Write(
                directory: tempPathForNugetConfigWithWhiteSpace,
                configname: nugetConfigName,
                localFeedPath: GetTestLocalFeedPath());

            return new FilePath(Path.GetFullPath(Path.Combine(tempPathForNugetConfigWithWhiteSpace, nugetConfigName)));
        }

        private static void DownloadPlatformsPackage()
        {
            if (File.Exists(Path.Combine(GetTestLocalFeedPath(), PlatformsNupkgFileName)))
            {

                return;
            }

            void download()
            {
                new WebClient().DownloadFile(PlatformsNupkgUri, Path.Combine(GetTestLocalFeedPath(), PlatformsNupkgFileName));
            }

            try
            {
                download();
            }
            catch (WebException)
            {
                download(); // naive retry once more
            }
        }

        private static ToolConfigurationAndExecutablePath RunInTransaction(IObtainTransaction obtainTransaction)
        {
            using (var t = new TransactionScope())
            {
                Transaction.Current.EnlistVolatile(obtainTransaction, EnlistmentOptions.None);
                var toolConfigurationAndExecutablePath = obtainTransaction.ObtainAndReturnExecutablePath();

                t.Complete();
                return toolConfigurationAndExecutablePath;
            }
        }

        private static string GetTestLocalFeedPath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestAssetLocalNugetFeed");

        private readonly string _testTargetframework = BundledTargetFramework.GetTargetFrameworkMoniker();
        private const string TestPackageVersion = "1.0.4";
        private const string TestPackageId = "global.tool.console.demo";
        private const string PlatformsNupkgFileName = "microsoft.netcore.platforms.2.1.0-preview1-26115-04.nupkg";
        private const string PlatformsNupkgUri = "https://dotnet.myget.org/F/dotnet-core/api/v2/package/Microsoft.NETCore.Platforms/2.1.0-preview1-26115-04";
    }
}
