// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class LockFileReaderTests : TestBase
    {
        [Fact]
        public void ReadLockFileReadsLibrariesWithPathProperty()
        {
            // Arrange
            var lockFileJson = @"
            {
              ""libraries"": {
                ""PackageA/1.0.1-Alpha"": {
                  ""sha512"": ""FAKE-HASH"",
                  ""type"": ""package"",
                  ""path"": ""PackageA/1.0.1-beta-PATH""
                },
                ""ProjectA/1.0.2-Beta"": {
                  ""type"": ""project"",
                  ""path"": ""ProjectA-PATH""
                }
              }
            }";

            var root = Temp.CreateDirectory();
            var file = root.CreateFile("project.lock.json");
            file.WriteAllText(lockFileJson);

            // Act
            var lockFile = LockFileReader.Read(file.Path, designTime: false);

            // Assert
            lockFile.PackageLibraries.Should().HaveCount(1);
            var package = lockFile.PackageLibraries.First();
            package.Name.Should().Be("PackageA");
            package.Version.ToString().Should().Be("1.0.1-Alpha");
            package.Sha512.Should().Be("FAKE-HASH");
            package.Path.Should().Be("PackageA/1.0.1-beta-PATH");

            lockFile.ProjectLibraries.Should().HaveCount(1);
            var project = lockFile.ProjectLibraries.First();
            project.Name.Should().Be("ProjectA");
            project.Version.ToString().Should().Be("1.0.2-Beta");
            project.Path.Should().Be("ProjectA-PATH");
        }

        [Fact]
        public void ReadLockFileReadsLibrariesWithoutPathProperty()
        {
            // Arrange
            var lockFileJson = @"
            {
              ""libraries"": {
                ""PackageA/1.0.1-Alpha"": {
                  ""sha512"": ""FAKE-HASH"",
                  ""type"": ""package""
                },
                ""ProjectA/1.0.2-Beta"": {
                  ""type"": ""project""
                }
              }
            }";

            var root = Temp.CreateDirectory();
            var file = root.CreateFile("project.lock.json");
            file.WriteAllText(lockFileJson);

            // Act
            var lockFile = LockFileReader.Read(file.Path, designTime: false);

            // Assert
            lockFile.PackageLibraries.Should().HaveCount(1);
            var package = lockFile.PackageLibraries.First();
            package.Name.Should().Be("PackageA");
            package.Version.ToString().Should().Be("1.0.1-Alpha");
            package.Sha512.Should().Be("FAKE-HASH");
            package.Path.Should().BeNull();

            lockFile.ProjectLibraries.Should().HaveCount(1);
            var project = lockFile.ProjectLibraries.First();
            project.Name.Should().Be("ProjectA");
            project.Version.ToString().Should().Be("1.0.2-Beta");
            project.Path.Should().BeNull();
        }
    }
}
