// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.ToolPackage;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.DotNet.Tools.Tests.ComponentMocks;
using Microsoft.Extensions.EnvironmentAbstractions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.ShellShim.Tests
{
    public class ShellShimRepositoryTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private Lazy<FilePath> _reusedHelloWorldExecutableDll = new Lazy<FilePath>(() => MakeHelloWorldExecutableDll("reused"));

        public ShellShimRepositoryTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFile()
        {
            var outputDll = _reusedHelloWorldExecutableDll.Value;
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            ShellShimRepository shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();

            shellShimRepository.CreateShim(outputDll, new ToolCommandName(shellCommandName));

            var stdOut = ExecuteInShell(shellCommandName, pathToShim);

            stdOut.Should().Contain("Hello World");
        }

        // Reproduce https://github.com/dotnet/cli/issues/9319
        [Fact]
        public void GivenAnExecutableAndRelativePathToShimPathItCanGenerateShimFile()
        {
            var outputDll = MakeHelloWorldExecutableDll("GivenAnExecutableAndRelativePath");
            // To reproduce the bug, dll need to be nested under the shim
            var parentPathAsShimPath = outputDll.GetDirectoryPath().GetParentPath().GetParentPath().Value;
            var relativePathToShim = Path.GetRelativePath(
                Directory.GetCurrentDirectory(),
                parentPathAsShimPath);

            ShellShimRepository shellShimRepository = ConfigBasicTestDependencyShellShimRepository(relativePathToShim);
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();

            shellShimRepository.CreateShim(outputDll, new ToolCommandName(shellCommandName));

            var stdOut = ExecuteInShell(shellCommandName, relativePathToShim);

            stdOut.Should().Contain("Hello World");
        }

        private static ShellShimRepository ConfigBasicTestDependencyShellShimRepository(string pathToShim)
        {
            string stage2AppHostTemplateDirectory = GetAppHostTemplateFromStage2();

            return new ShellShimRepository(new DirectoryPath(pathToShim), stage2AppHostTemplateDirectory);
        }

        [Fact]
        public void GivenAnExecutablePathItCanGenerateShimFileInTransaction()
        {
            var outputDll = _reusedHelloWorldExecutableDll.Value;
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            var shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();

            using (var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                TimeSpan.Zero))
            {
                shellShimRepository.CreateShim(outputDll, new ToolCommandName(shellCommandName));
                transactionScope.Complete();
            }

            var stdOut = ExecuteInShell(shellCommandName, pathToShim);

            stdOut.Should().Contain("Hello World");
        }

        [Fact]
        public void GivenAnExecutablePathDirectoryThatDoesNotExistItCanGenerateShimFile()
        {
            var outputDll = _reusedHelloWorldExecutableDll.Value;
            var extraNonExistDirectory = Path.GetRandomFileName();
            var shellShimRepository = new ShellShimRepository(new DirectoryPath(Path.Combine(TempRoot.Root, extraNonExistDirectory)), GetAppHostTemplateFromStage2());
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();

            Action a = () => shellShimRepository.CreateShim(outputDll, new ToolCommandName(shellCommandName));

            a.ShouldNotThrow<DirectoryNotFoundException>();
        }

        [Theory]
        [InlineData("arg1 arg2", new[] { "arg1", "arg2" })]
        [InlineData(" \"arg1 with space\" arg2", new[] { "arg1 with space", "arg2" })]
        [InlineData(" \"arg with ' quote\" ", new[] { "arg with ' quote" })]
        public void GivenAShimItPassesThroughArguments(string arguments, string[] expectedPassThru)
        {
            var outputDll = _reusedHelloWorldExecutableDll.Value;
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            var shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();

            shellShimRepository.CreateShim(outputDll, new ToolCommandName(shellCommandName));

            var stdOut = ExecuteInShell(shellCommandName, pathToShim, arguments);

            for (int i = 0; i < expectedPassThru.Length; i++)
            {
                stdOut.Should().Contain($"{i} = {expectedPassThru[i]}");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAShimConflictItWillRollback(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            MakeNameConflictingCommand(pathToShim, shellCommandName);

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Action a = () =>
            {
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    shellShimRepository.CreateShim(new FilePath("dummy.dll"), new ToolCommandName(shellCommandName));

                    scope.Complete();
                }
            };

            a.ShouldThrow<ShellShimException>().Where(
                ex => ex.Message ==
                    string.Format(
                        CommonLocalizableStrings.ShellShimConflict,
                        shellCommandName));

            Directory
                .EnumerateFileSystemEntries(pathToShim)
                .Should()
                .HaveCount(1, "should only be the original conflicting command");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnExceptionItWillRollback(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Action intendedError = () => throw new ToolPackageException("simulated error");

            Action a = () =>
            {
                using (var scope = new TransactionScope(
                    TransactionScopeOption.Required,
                    TimeSpan.Zero))
                {
                    FilePath targetExecutablePath = _reusedHelloWorldExecutableDll.Value;
                    shellShimRepository.CreateShim(targetExecutablePath, new ToolCommandName(shellCommandName));

                    intendedError();
                    scope.Complete();
                }
            };
            a.ShouldThrow<ToolPackageException>().WithMessage("simulated error");

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenANonexistentShimRemoveDoesNotThrow(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();

            shellShimRepository.RemoveShim(new ToolCommandName(shellCommandName));

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledShimRemoveDeletesTheShimFiles(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();

            FilePath targetExecutablePath = _reusedHelloWorldExecutableDll.Value;
            shellShimRepository.CreateShim(targetExecutablePath, new ToolCommandName(shellCommandName));

            Directory.EnumerateFileSystemEntries(pathToShim).Should().NotBeEmpty();

            shellShimRepository.RemoveShim(new ToolCommandName(shellCommandName));

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledShimRemoveRollsbackIfTransactionIsAborted(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();

            FilePath targetExecutablePath = _reusedHelloWorldExecutableDll.Value;
            shellShimRepository.CreateShim(targetExecutablePath, new ToolCommandName(shellCommandName));

            Directory.EnumerateFileSystemEntries(pathToShim).Should().NotBeEmpty();

            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                TimeSpan.Zero))
            {
                shellShimRepository.RemoveShim(new ToolCommandName(shellCommandName));

                Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GivenAnInstalledShimRemoveCommitsIfTransactionIsCompleted(bool testMockBehaviorIsInSync)
        {
            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();

            IShellShimRepository shellShimRepository;
            if (testMockBehaviorIsInSync)
            {
                shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);
            }
            else
            {
                shellShimRepository = ConfigBasicTestDependencyShellShimRepository(pathToShim);
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();

            FilePath targetExecutablePath = _reusedHelloWorldExecutableDll.Value;
            shellShimRepository.CreateShim(targetExecutablePath, new ToolCommandName(shellCommandName));

            Directory.EnumerateFileSystemEntries(pathToShim).Should().NotBeEmpty();

            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                TimeSpan.Zero))
            {
                shellShimRepository.RemoveShim(new ToolCommandName(shellCommandName));

                Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();

                scope.Complete();
            }

            Directory.EnumerateFileSystemEntries(pathToShim).Should().BeEmpty();
        }

        [Fact]
        public void WhenPackagedShimProvidedItCopies()
        {
            const string tokenToIdentifyCopiedShim = "packagedShim";

            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            var packagedShimFolder = GetNewCleanFolderUnderTempRoot();
            var dummyShimPath = Path.Combine(packagedShimFolder, shellCommandName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dummyShimPath = dummyShimPath + ".exe";
            }

            File.WriteAllText(dummyShimPath, tokenToIdentifyCopiedShim);

            ShellShimRepository shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);

            shellShimRepository.CreateShim(
                new FilePath("dummy.dll"),
                new ToolCommandName(shellCommandName),
                new[] {new FilePath(dummyShimPath)});

            var createdShim = Directory.EnumerateFileSystemEntries(pathToShim).Single();
            File.ReadAllText(createdShim).Should().Contain(tokenToIdentifyCopiedShim);
        }

        [Fact]
        public void WhenMultipleSameNamePackagedShimProvidedItThrows()
        {
            const string tokenToIdentifyCopiedShim = "packagedShim";

            var shellCommandName = nameof(ShellShimRepositoryTests) + Path.GetRandomFileName();
            var pathToShim = GetNewCleanFolderUnderTempRoot();
            var packagedShimFolder = GetNewCleanFolderUnderTempRoot();
            var dummyShimPath = Path.Combine(packagedShimFolder, shellCommandName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dummyShimPath = dummyShimPath + ".exe";
            }

            File.WriteAllText(dummyShimPath, tokenToIdentifyCopiedShim);
            ShellShimRepository shellShimRepository = GetShellShimRepositoryWithMockMaker(pathToShim);

            FilePath[] filePaths = new[] { new FilePath(dummyShimPath), new FilePath("path" + dummyShimPath) };

            Action a = () => shellShimRepository.CreateShim(
                new FilePath("dummy.dll"),
                new ToolCommandName(shellCommandName),
                new[] { new FilePath(dummyShimPath), new FilePath("path" + dummyShimPath) });

            a.ShouldThrow<ShellShimException>()
                .And.Message
                .Should().Contain(
                    string.Format(
                           CommonLocalizableStrings.MoreThanOnePackagedShimAvailable,
                           string.Join(';', filePaths)));
        }

        private static void MakeNameConflictingCommand(string pathToPlaceShim, string shellCommandName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shellCommandName = shellCommandName + ".exe";
            }

            File.WriteAllText(Path.Combine(pathToPlaceShim, shellCommandName), string.Empty);
        }

        private string ExecuteInShell(string shellCommandName, string cleanFolderUnderTempRoot, string arguments = "")
        {
            ProcessStartInfo processStartInfo;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var file = Path.Combine(cleanFolderUnderTempRoot, shellCommandName + ".exe");
                processStartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = false,
                    Arguments = arguments,
                };
            }
            else
            {
                var file = Path.Combine(cleanFolderUnderTempRoot, shellCommandName);
                processStartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = arguments,
                    UseShellExecute = false
                };
            }

            _output.WriteLine($"Launching '{processStartInfo.FileName} {processStartInfo.Arguments}'");
            processStartInfo.WorkingDirectory = cleanFolderUnderTempRoot;

            var environmentProvider = new EnvironmentProvider();
            processStartInfo.EnvironmentVariables["PATH"] = environmentProvider.GetEnvironmentVariable("PATH");
            if (Environment.Is64BitProcess)
            {
                processStartInfo.EnvironmentVariables["DOTNET_ROOT"] = new RepoDirectoriesProvider().DotnetRoot;
            }
            else
            {
                processStartInfo.EnvironmentVariables["DOTNET_ROOT(x86)"] = new RepoDirectoriesProvider().DotnetRoot;
            }

            processStartInfo.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);

            stdErr.Should().BeEmpty();

            return stdOut ?? "";
        }

        private static string GetAppHostTemplateFromStage2()
        {
            var stage2AppHostTemplateDirectory =
                new DirectoryInfo(new RepoDirectoriesProvider().Stage2Sdk)
                .GetDirectory("AppHostTemplate").FullName;
            return stage2AppHostTemplateDirectory;
        }

        private static FilePath MakeHelloWorldExecutableDll(string instanceName = null)
        {
            const string testAppName = "TestAppSimple";
            const string emptySpaceToTestSpaceInPath = " ";
            const string directoryNamePostFix = "Test";

            if (instanceName == null)
            {
                instanceName = testAppName + emptySpaceToTestSpaceInPath + directoryNamePostFix;
            }

            TestAssetInstance testInstance = TestAssets.Get(testAppName)
                .CreateInstance(instanceName)
                .WithRestoreFiles()
                .WithBuildFiles();

            var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";

            FileInfo outputDll = testInstance.Root.GetDirectory("bin", configuration)
                .EnumerateDirectories()
                .Single()
                .GetFile($"{testAppName}.dll");

            return new FilePath(outputDll.FullName);
        }

        private static string GetNewCleanFolderUnderTempRoot()
        {
            DirectoryInfo CleanFolderUnderTempRoot = new DirectoryInfo(Path.Combine(TempRoot.Root, "cleanfolder" + Path.GetRandomFileName()));
            CleanFolderUnderTempRoot.Create();

            return CleanFolderUnderTempRoot.FullName;
        }

        private ShellShimRepository GetShellShimRepositoryWithMockMaker(string pathToShim)
        {
            return new ShellShimRepository(
                    new DirectoryPath(pathToShim),
                    appHostShellShimMaker: new AppHostShellShimMakerMock());
        }
    }
}
