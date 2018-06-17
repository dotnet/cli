using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace EndToEnd
{
    public class GivenPortableApps : TestBase
    {
        [Theory]
        [ClassData(typeof(SupportedNetCoreAppVersions))]
        public void ItDoesNotRollsForwardToTheLatestVersion(string minorVersion)
        {
            var _testInstance = TestAssets.Get("TestAppSimple")
                .CreateInstance(identifier: minorVersion)
                .WithSourceFiles();

            string projectDirectory = _testInstance.Root.FullName;

            string projectPath = Path.Combine(projectDirectory, "TestAppSimple.csproj");

            var project = XDocument.Load(projectPath);
            var ns = project.Root.Name.Namespace;

            //  Update TargetFramework to the right version of .NET Core
            project.Root.Element(ns + "PropertyGroup")
                .Element(ns + "TargetFramework")
                .Value = "netcoreapp" + minorVersion;

            project.Save(projectPath);

            //  Get the version rolled forward to
            new RestoreCommand()
                    .WithWorkingDirectory(projectDirectory)
                    .Execute()
                    .Should().Pass();

            string assetsFilePath = Path.Combine(projectDirectory, "obj", "project.assets.json");
            var assetsFile = new LockFileFormat().Read(assetsFilePath);

            var versionInAssertsJson = GetNetCoreAppVersion(assetsFile);
            versionInAssertsJson.Should().NotBeNull();

            if (versionInAssertsJson.IsPrerelease)
            {
                //  If this version of .NET Core is still prerelease, then:
                //  - There aren't any patches to roll-forward to, so we skip testing this until the version
                //    leaves prerelease.
                return;
            }

            versionInAssertsJson.ToNormalizedString().Should().BeEquivalentTo(GetExpectedVersion(minorVersion));
        }

        private NuGetVersion GetNetCoreAppVersion(LockFile lockFile)
        {
            return lockFile?.Targets?.SingleOrDefault(t => t.RuntimeIdentifier == null)
                ?.Libraries?.SingleOrDefault(l =>
                    string.Compare(l.Name, "Microsoft.NETCore.App", StringComparison.CurrentCultureIgnoreCase) == 0)
                ?.Version;
        }

        public string GetExpectedVersion(string minorVersion)
        {
            if (minorVersion.StartsWith("1.0"))
            {
                return "1.0.5";  // special case for 1.0
            }
            else if (minorVersion.StartsWith("1.1"))
            {
                return "1.1.2";  // special case for 1.1
            }
            else
            {
                var parsed = NuGetVersion.Parse(minorVersion);
                return new NuGetVersion(parsed.Major, parsed.Minor, 0).ToNormalizedString();
            }
        }
    }
}
