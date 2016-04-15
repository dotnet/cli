﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.ProjectModel.Server.Tests
{
    public class DthTests : TestBase
    {
        private readonly TestAssetsManager _testAssetsManager;
        private readonly ILoggerFactory _loggerFactory;

        public DthTests()
        {
            _loggerFactory = new LoggerFactory();

            var testVerbose = Environment.GetEnvironmentVariable("DOTNET_TEST_VERBOSE");
            if (testVerbose == "2")
            {
                _loggerFactory.AddConsole(LogLevel.Trace);
            }
            else if (testVerbose == "1")
            {
                _loggerFactory.AddConsole(LogLevel.Information);
            }
            else if (testVerbose == "0")
            {
                _loggerFactory.AddConsole(LogLevel.Warning);
            }
            else
            {
                _loggerFactory.AddConsole(LogLevel.Error);
            }

            _testAssetsManager = new TestAssetsManager(
                Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer", "DthTestProjects", "src"));
        }

        [Fact]
        public void DthStartup_GetProjectInformation()
        {
            var projectPath = Path.Combine(_testAssetsManager.AssetsRoot, "EmptyConsoleApp");
            Assert.NotNull(projectPath);

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);

                var projectInformation = client.DrainTillFirst(MessageTypes.ProjectInformation)
                                               .EnsureSource(server, client)
                                               .RetrievePayloadAs<JObject>()
                                               .AssertProperty("Name", "EmptyConsoleApp");

                projectInformation.RetrievePropertyAs<JArray>("Configurations")
                                  .AssertJArrayCount(2)
                                  .AssertJArrayContains("Debug")
                                  .AssertJArrayContains("Release");

                var frameworkShortNames = projectInformation.RetrievePropertyAs<JArray>("Frameworks")
                                                            .AssertJArrayCount(2)
                                                            .Select(f => f["ShortName"].Value<string>());

                Assert.Contains("netstandardapp1.5", frameworkShortNames);
                Assert.Contains("dnx451", frameworkShortNames);
            }
        }

        [Fact]
        public void DependencyDiagnsoticsAfterDependencies()
        {
            var projectPath = Path.Combine(_testAssetsManager.AssetsRoot, "EmptyConsoleApp");
            Assert.NotNull(projectPath);

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);
                var messages = client.DrainAllMessages()
                                     .Select(message => message.MessageType)
                                     .ToArray();

                var expectDependencies = true;
                var expectDependencyDiagnostics = false;
                for (var i = 0; i < messages.Length; ++i)
                {
                    if (messages[i] == MessageTypes.Dependencies)
                    {
                        Assert.True(expectDependencies);
                        expectDependencies = false;
                        expectDependencyDiagnostics = true;
                    }
                    else if (messages[i] == MessageTypes.DependencyDiagnostics)
                    {
                        Assert.True(expectDependencyDiagnostics);
                        expectDependencyDiagnostics = false;
                        break;
                    }
                }

                Assert.False(expectDependencies);
                Assert.False(expectDependencyDiagnostics);
            }
        }

        [Theory]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        [InlineData(3, 3)]
        public void DthStartup_ProtocolNegotiation(int requestVersion, int expectVersion)
        {
            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.SetProtocolVersion(requestVersion);

                var response = client.DrainTillFirst(MessageTypes.ProtocolVersion, TimeSpan.FromDays(1));
                response.EnsureSource(server, client);

                Assert.Equal(expectVersion, response.Payload["Version"]?.Value<int>());
            }
        }

        [Fact]
        public void DthStartup_ProtocolNegotiation_ZeroIsNoAllowed()
        {
            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.SetProtocolVersion(0);

                Assert.Throws<TimeoutException>(() =>
                {
                    client.DrainTillFirst(MessageTypes.ProtocolVersion, timeout: TimeSpan.FromSeconds(1));
                });
            }
        }

        [Theory]
        [InlineData("Project", "UnresolvedProjectSample", "EmptyLibrary", "Project")]
        [InlineData("Package", "UnresolvedPackageSample", "NoSuchPackage", null)]
        [InlineData("Package", "IncompatiblePackageSample", "Microsoft.Web.Administration", "Package")]
        public void DthCompilation_Initialize_UnresolvedDependency(string referenceType,
                                                                   string testProjectName,
                                                                   string expectedUnresolvedDependency,
                                                                   string expectedUnresolvedType)
        {
            if (PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Linux)
            {
                Console.WriteLine("Test is skipped on Linux");
                return;
            }

            var projectPath = Path.Combine(_testAssetsManager.AssetsRoot, testProjectName);
            Assert.NotNull(projectPath);

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);

                var referencesMessage = client.DrainTillFirst(MessageTypes.References, TimeSpan.FromDays(1))
                                              .EnsureSource(server, client);

                if (referenceType == "Project")
                {
                    var expectedUnresolvedProjectPath = Path.Combine(Path.GetDirectoryName(projectPath),
                                                                     expectedUnresolvedDependency,
                                                                     Project.FileName);

                    referencesMessage.RetrievePayloadAs<JObject>()
                                     .RetrievePropertyAs<JArray>("ProjectReferences")
                                     .AssertJArrayCount(1)
                                     .RetrieveArraryElementAs<JObject>(0)
                                     .AssertProperty("Name", expectedUnresolvedDependency)
                                     .AssertProperty("Path", expectedUnresolvedProjectPath);
                }
                else if (referenceType == "Package")
                {
                    referencesMessage.RetrievePayloadAs<JObject>()
                                     .RetrievePropertyAs<JArray>("ProjectReferences")
                                     .AssertJArrayCount(0);
                }

                var unresolveDependency = client.DrainTillFirst(MessageTypes.Dependencies)
                                                .EnsureSource(server, client)
                                                .RetrieveDependency(expectedUnresolvedDependency);

                unresolveDependency.AssertProperty("Name", expectedUnresolvedDependency)
                                   .AssertProperty("DisplayName", expectedUnresolvedDependency)
                                   .AssertProperty("Resolved", false)
                                   .AssertProperty("Type", expectedUnresolvedType);

                if (expectedUnresolvedType == "Project")
                {
                    unresolveDependency.AssertProperty("Path", Path.Combine(Path.GetDirectoryName(projectPath),
                                                                            expectedUnresolvedDependency,
                                                                            Project.FileName));
                }
                else
                {
                    Assert.False(unresolveDependency["Path"].HasValues);
                }
            }
        }

        [Fact]
        public void DthNegative_BrokenProjectPathInLockFile()
        {
            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                // After restore the project is copied to another place so that
                // the relative path in project lock file is invalid.
                var movedProjectPath = _testAssetsManager.CreateTestInstance("BrokenProjectPathSample")
                                                         .WithLockFiles()
                                                         .TestRoot;

                client.Initialize(movedProjectPath);

                client.DrainTillFirst("Dependencies")
                      .RetrieveDependency("EmptyLibrary")
                      .AssertProperty<JArray>("Errors", errorsArray => errorsArray.Count == 1)
                      .AssertProperty<JArray>("Warnings", warningsArray => warningsArray.Count == 0)
                      .AssertProperty("Name", "EmptyLibrary")
                      .AssertProperty("Resolved", false);

                client.DrainTillFirst("DependencyDiagnostics")
                      .RetrieveDependencyDiagnosticsCollection()
                      .RetrieveDependencyDiagnosticsErrorAt(0)
                      .AssertProperty<string>("FormattedMessage", message => message.Contains("error NU1002"))
                      .RetrievePropertyAs<JObject>("Source")
                      .AssertProperty("Name", "EmptyLibrary");
            }
        }

        [Fact(Skip = "Require dotnet restore integration test")]
        public void DthDependencies_UpdateGlobalJson_RefreshDependencies()
        {
            var assets = new TestAssetsManager(Path.Combine(AppContext.BaseDirectory, "TestAssets", "ProjectModelServer"));
            var projectPath = assets.CreateTestInstance("DthUpdateSearchPathSample").WithLockFiles().TestRoot;
            Assert.True(Directory.Exists(projectPath));

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                var testProject = Path.Combine(projectPath, "home", "src", "MainProject");

                client.Initialize(testProject);

                client.DrainTillFirst("ProjectInformation")
                      .RetrievePayloadAs<JObject>()
                      .RetrievePropertyAs<JArray>("ProjectSearchPaths")
                      .AssertJArrayCount(2);

                client.DrainTillFirst("Dependencies")
                      .RetrieveDependency("Newtonsoft.Json")
                      .AssertProperty("Type", "Project")
                      .AssertProperty("Resolved", true)
                      .AssertProperty<JArray>("Errors", array => array.Count == 0, _ => "Dependency shouldn't contain any error.");

                client.DrainTillFirst("DependencyDiagnostics")
                      .RetrievePayloadAs<JObject>()
                      .AssertProperty<JArray>("Errors", array => array.Count == 0)
                      .AssertProperty<JArray>("Warnings", array => array.Count == 0);

                // Overwrite the global.json to remove search path to ext
                File.WriteAllText(
                    Path.Combine(projectPath, "home", GlobalSettings.FileName),
                    JsonConvert.SerializeObject(new { project = new string[] { "src" } }));

                client.SendPayLoad(testProject, "RefreshDependencies");

                client.DrainTillFirst("ProjectInformation")
                      .RetrievePayloadAs<JObject>()
                      .RetrievePropertyAs<JArray>("ProjectSearchPaths")
                      .AssertJArrayCount(1)
                      .AssertJArrayElement(0, Path.Combine(projectPath, "home", "src"));

                client.DrainTillFirst("Dependencies")
                      .RetrieveDependency("Newtonsoft.Json")
                      .AssertProperty("Type", "")
                      .AssertProperty("Resolved", false)
                      .RetrievePropertyAs<JArray>("Errors")
                      .AssertJArrayCount(1)
                      .RetrieveArraryElementAs<JObject>(0)
                      .AssertProperty("ErrorCode", "NU1010");

                client.DrainTillFirst("DependencyDiagnostics")
                      .RetrieveDependencyDiagnosticsCollection()
                      .RetrieveDependencyDiagnosticsErrorAt<JObject>(0)
                      .AssertProperty("ErrorCode", "NU1010");
            }
        }

        [Fact]
        public void DthStartup_OpenProjectBeforeRestore()
        {
            var projectPath = _testAssetsManager.CreateTestInstance("EmptyConsoleApp").TestRoot;

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);
                var messages = client.DrainAllMessages();
                Assert.False(messages.Any(msg => msg.MessageType == MessageTypes.Error));

                var dependencyDiagnostics = messages.Where(msg => msg.MessageType == MessageTypes.DependencyDiagnostics);
                Assert.Equal(2, dependencyDiagnostics.Count());

                foreach (var message in dependencyDiagnostics)
                {
                    message.RetrievePayloadAs<JObject>()
                           .RetrievePropertyAs<JArray>("Errors")
                           .AssertJArrayContains<JObject>(error => error["ErrorCode"].Value<string>() == ErrorCodes.NU1009);
                }
            }
        }

        [Fact]
        public void InvalidProjectJson()
        {
            var testAssetsPath = Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer");
            var assetsManager = new TestAssetsManager(testAssetsPath);
            var testSource = assetsManager.CreateTestInstance("IncorrectProjectJson").TestRoot;

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(Path.Combine(_testAssetsManager.AssetsRoot, "EmptyLibrary"));
                client.Initialize(testSource);

                // Error for invalid project.json
                var messages = client.DrainAllMessages();
                messages.Single(msg => msg.MessageType == MessageTypes.Error)
                        .Payload.AsJObject()
                        .AssertProperty<string>("Path", v => v.Contains("IncorrectProjectJson"));

                // Successfully initialize the other project
                messages.Single(msg => msg.MessageType == MessageTypes.ProjectInformation)
                        .Payload.AsJObject()
                        .AssertProperty<string>("Name", v => string.Equals(v, "EmptyLibrary", StringComparison.Ordinal));

                // Successfully initialize another project afterwards
                client.Initialize(Path.Combine(_testAssetsManager.AssetsRoot, "EmptyConsoleApp"));
                messages = client.DrainAllMessages();
                messages.Single(msg => msg.MessageType == MessageTypes.ProjectInformation)
                        .Payload.AsJObject()
                        .AssertProperty<string>("Name", v => string.Equals(v, "EmptyConsoleApp", StringComparison.Ordinal));
            }
        }

        [Fact]
        public void InvalidGlobalJson()
        {
            var testAssetsPath = Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer");
            var assetsManager = new TestAssetsManager(testAssetsPath);
            var testSource = assetsManager.CreateTestInstance("IncorrectGlobalJson");

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(Path.Combine(testSource.TestRoot, "src", "Project1"));

                var messages = client.DrainAllMessages();
                messages.ContainsMessage(MessageTypes.Error)
                        .Single().Payload.AsJObject()
                        .AssertProperty<string>("Path", v => v.Contains("InvalidGlobalJson"));
            }
        }

        [Fact]
        public void RecoverFromGlobalError()
        {
            var testProject = _testAssetsManager.CreateTestInstance("EmptyConsoleApp")
                                                .WithLockFiles()
                                                .TestRoot;

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                var projectFile = Path.Combine(testProject, Project.FileName);
                var content = File.ReadAllText(projectFile);
                File.WriteAllText(projectFile, content + "}");

                client.Initialize(testProject);
                var messages = client.DrainAllMessages();
                messages.ContainsMessage(MessageTypes.Error);

                File.WriteAllText(projectFile, content);
                client.SendPayLoad(testProject, MessageTypes.FilesChanged);
                var clearError = client.DrainTillFirst(MessageTypes.Error);
                clearError.Payload.AsJObject().AssertProperty("Message", null as string);
            }
        }

        [Theory]
        [InlineData(500, true)]
        [InlineData(3000, false)]
        public void WaitForLockFileReleased(int occupyFileFor, bool expectSuccess)
        {
            var testProject = _testAssetsManager.CreateTestInstance("EmptyConsoleApp")
                                                .WithLockFiles()
                                                .TestRoot;

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                var lockFilePath = Path.Combine(testProject, LockFile.FileName);
                var lockFileContent = File.ReadAllText(lockFilePath);
                var fs = new FileStream(lockFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

                // Test the platform
                // A sharing violation is expected in following code. Otherwise the FileSteam is not implemented correctly.
                Assert.ThrowsAny<IOException>(() =>
                {
                    new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                });

                var task = Task.Run(() =>
                {
                    // WorkspaceContext will try to open the lock file for 3 times with 500 ms interval in between.
                    Thread.Sleep(occupyFileFor);
                    fs.Dispose();
                });

                client.Initialize(testProject);
                var messages = client.DrainAllMessages();
                if (expectSuccess)
                {
                    messages.AssertDoesNotContain(MessageTypes.Error);
                }
                else
                {
                    messages.ContainsMessage(MessageTypes.Error);
                }
            }
        }

        [Fact]
        public void AddMSBuildReferenceBeforeRestore()
        {
            var tam = new TestAssetsManager(
                Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer", "MSBuildReferencesProjects"));

            // var appName = "EmptyNetCoreApp";
            var projectPath = tam.CreateTestInstance("ValidCase01").WithLockFiles().TestRoot;
            projectPath = Path.Combine(projectPath, "src", "MainApp");

            var projectFilePath = Path.Combine(projectPath, Project.FileName);
            var projectJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(projectFilePath));

            ((JObject)projectJson["frameworks"]["net46"]["dependencies"])
                .Add("ClassLibrary4", JToken.FromObject(new { target = "project" }));

            File.WriteAllText(projectFilePath, JsonConvert.SerializeObject(projectJson));

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);
                var messages = client.DrainAllMessages();
                messages.AssertDoesNotContain(MessageTypes.Error);
                // PrintAllMessages(new[] { messages.RetrieveSingleMessage(MessageTypes.Dependencies) });
                messages.RetrieveSingleMessage(MessageTypes.Dependencies)
                        .RetrieveDependency("ClassLibrary4")
                        .AssertProperty<object>(
                            "Version",
                            v => !string.IsNullOrEmpty(v.ToString()),
                            v => $"Version string shouldn't be empty. Value [{v.ToString()}]");
            }
        }

        [Fact]
        public void MSBuildReferenceTest()
        {
            var testProject = Path.Combine(RepoRoot, "TestAssets",
                                                     "ProjectModelServer",
                                                     "MSBuildReferencesProjects",
                                                     "ValidCase01",
                                                     "src",
                                                     "MainApp");

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(testProject);
                var messages = client.DrainAllMessages();

                var classLibraries = new HashSet<string>(new string[] { "ClassLibrary1", "ClassLibrary2", "ClassLibrary3" });
                var dependencies = messages.RetrieveSingleMessage(MessageTypes.Dependencies);
                var testProjectRoot = Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer", "MSBuildReferencesProjects", "ValidCase01");
                foreach (var classLibrary in classLibraries)
                {
                    dependencies.RetrieveDependency(classLibrary)
                                .AssertProperty("Type", LibraryType.MSBuildProject.ToString())
                                .AssertProperty("Path", NormalizePathString(Path.Combine(testProjectRoot, classLibrary, $"{classLibrary}.csproj")))
                                .AssertProperty<bool>("Resolved", true)
                                .AssertProperty("Name", classLibrary)
                                .AssertProperty<JArray>("Errors", array => array.Count == 0)
                                .AssertProperty<JArray>("Warnings", array => array.Count == 0);
                }

                var references = messages.RetrieveSingleMessage(MessageTypes.References)
                                         .RetrievePayloadAs<JObject>();

                var projectReferences = references.RetrievePropertyAs<JArray>("ProjectReferences");
                Assert.Equal(3, projectReferences.Count);
                for (int i = 0; i < 3; ++i)
                {
                    var projectRef = projectReferences.RetrieveArraryElementAs<JObject>(i);
                    var name = projectRef["Name"].Value<string>();

                    Assert.True(classLibraries.Contains(name));
                    projectRef.AssertProperty("Path", NormalizePathString(Path.Combine(testProjectRoot, name, $"{name}.csproj")));
                }

                var fileReferences = references.RetrievePropertyAs<JArray>("FileReferences")
                                               .Select(each => each.Value<string>())
                                               .ToArray();
                foreach (var each in classLibraries)
                {
                    fileReferences.Contains(Path.Combine("ValidCase01", "ClassLibrary1", "bin", "Debug", $"{each}.dll"));
                }
            }
        }

        [Fact]
        public void RemovePackageDependencyFromProjectJson()
        {
            // Remove a package dependency from project.json and then request refreshing dependency before
            // restore.

            var appName = "EmptyNetCoreApp";
            var projectPath = _testAssetsManager.CreateTestInstance(appName)
                                                .WithLockFiles()
                                                .TestRoot;

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);

                client.DrainAllMessages()
                      .AssertDoesNotContain(MessageTypes.Error)
                      .RetrieveSingleMessage(MessageTypes.Dependencies)
                      .RetrieveDependency(appName)
                      .RetrievePropertyAs<JArray>("Dependencies")
                      .AssertJArrayCount(2);

                var projectFilePath = Path.Combine(projectPath, Project.FileName);
                var projectJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(projectFilePath));

                // Remove newtonsoft.json dependency
                var dependencies = projectJson["frameworks"]["netcoreapp1.0"]["dependencies"] as JObject;
                dependencies.Remove("Newtonsoft.Json");

                File.WriteAllText(projectFilePath, JsonConvert.SerializeObject(projectJson));

                client.SendPayLoad(projectPath, MessageTypes.RefreshDependencies);

                var afterDependencies = client.DrainTillFirst(MessageTypes.Dependencies);
                afterDependencies.RetrieveDependency(appName)
                                 .RetrievePropertyAs<JArray>("Dependencies")
                                 .AssertJArrayCount(1)
                                 .RetrieveArraryElementAs<JObject>(0)
                                 .AssertProperty("Name", "Microsoft.NETCore.App");
                afterDependencies.RetrieveDependency("Newtonsoft.Json");
            }
        }

        [Fact]
        public void RemoveMSBuildDependencyFromProjectJson()
        {
            // Remove a msbuild project dependency from project.json and then request refreshing dependency before
            // restore.

            var tam = new TestAssetsManager(
                Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer", "MSBuildReferencesProjects"));

            // var appName = "EmptyNetCoreApp";
            var projectPath = tam.CreateTestInstance("ValidCase01").WithLockFiles().TestRoot;
            projectPath = Path.Combine(projectPath, "src", "MainApp");

            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);

                client.DrainAllMessages()
                      .AssertDoesNotContain(MessageTypes.Error)
                      .RetrieveSingleMessage(MessageTypes.Dependencies)
                      .RetrieveDependency("MainApp")
                      .RetrievePropertyAs<JArray>("Dependencies")
                      .AssertJArrayContains<JObject>(dep => dep["Name"].Value<string>() == "ClassLibrary1")
                      .AssertJArrayContains<JObject>(dep => dep["Name"].Value<string>() == "ClassLibrary2")
                      .AssertJArrayContains<JObject>(dep => dep["Name"].Value<string>() == "ClassLibrary3");

                var projectFilePath = Path.Combine(projectPath, Project.FileName);
                var projectJson = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(projectFilePath));

                // Remove ClassLibrary2 and ClassLibrary3 dependency
                var dependencies = projectJson["frameworks"]["net46"]["dependencies"] as JObject;
                dependencies.Remove("ClassLibrary2");
                dependencies.Remove("ClassLibrary3");

                File.WriteAllText(projectFilePath, JsonConvert.SerializeObject(projectJson));

                client.SendPayLoad(projectPath, MessageTypes.RefreshDependencies);

                var afterDependencies = client.DrainTillFirst(MessageTypes.Dependencies);
                afterDependencies.RetrieveDependency("MainApp")
                                 .RetrievePropertyAs<JArray>("Dependencies")
                                 .AssertJArrayNotContains<JObject>(dep => dep["Name"].Value<string>() == "ClassLibrary2")
                                 .AssertJArrayNotContains<JObject>(dep => dep["Name"].Value<string>() == "ClassLibrary3");

                afterDependencies.RetrieveDependency("ClassLibrary2");
                afterDependencies.RetrieveDependency("ClassLibrary3");
            }
        }
        
        [Fact]
        public void TestMscorlibLibraryDuplication()
        {
            var projectPath = Path.Combine(RepoRoot, "TestAssets", "ProjectModelServer", "MscorlibLibraryDuplication");
            
            using (var server = new DthTestServer(_loggerFactory))
            using (var client = new DthTestClient(server, _loggerFactory))
            {
                client.Initialize(projectPath);

                var messages = client.DrainAllMessages();
                messages.AssertDoesNotContain(MessageTypes.Error);
            }
        }

        private static string NormalizePathString(string original)
        {
            return original.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        private static void PrintAllMessages(IEnumerable<DthMessage> messages)
        {
            foreach (var message in messages)
            {
                Console.WriteLine($"{message.MessageType} => {message.Payload.ToString()}");
            }
        }
    }
}
