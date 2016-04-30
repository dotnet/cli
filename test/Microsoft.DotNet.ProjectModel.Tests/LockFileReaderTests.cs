// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.ProjectModel.Graph;
using Xunit;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Tests
{
    public class LockFileReaderTests
    {
        [Theory]
        [InlineData("", null)]
        [InlineData(@"""path"": null,", null)]
        [InlineData(@"""path"": ""foo/1.0.0"",", "foo/1.0.0")]
        public void AllowsPackageLibraryPath(string pathProperty, string expected)
        {
            // Arrange
            var reader = new LockFileReader();
            var lockFileJson = @"
            {
              ""libraries"": {
                ""Foo/1.0.0"": {
                  ""sha512"": ""something"",
                 " + pathProperty + @"
                  ""type"": ""package"",
                  ""files"": [
                    ""lib/netstandard1.0/Foo.dll""
                  ]
                }
              }
            }
            ";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(lockFileJson));

            // Act
            var lockFile = reader.ReadLockFile("project.lock.json", memoryStream, false);

            // Assert
            lockFile.PackageLibraries.Should().HaveCount(1);
            var library = lockFile.PackageLibraries[0];
            library.Path.Should().Be(expected);

            library.Name.Should().Be("Foo");
            library.Version.Should().Be(new NuGetVersion("1.0.0"));
            library.Sha512.Should().Be("something");
            library.Files.Should().HaveCount(1);
            library.Files[0].Should().Be(Path.Combine("lib", "netstandard1.0", "Foo.dll"));
        }
    }
}
