// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.DotNet.Kestrel.Tests
{
    public class DotnetRunTest : TestBase
    {
        [Fact]
        public void ItRunsKestrelPortableApp()
        {
            var instance = TestAssets.Get("KestrelSample")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var runCommand = new RunCommand(instance.Root.GetDirectory("KestrelPortable"));

            try
            {
                runCommand.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelPortable} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                runCommand.KillTree();
            }
        }

        [Fact]
        public void ItRunsKestrelStandaloneApp()
        {
            var instance = TestAssets.Get("KestrelSample")
                .CreateInstance()
                .WithSourceFiles()
                .WithRestoreFiles();

            var url = NetworkHelper.GetLocalhostUrlWithFreePort();
            var args = $"{url} {Guid.NewGuid().ToString()}";
            var runCommand = new RunCommand(instance.Root.GetDirectory("KestrelStandalone"));

            try
            {
                runCommand.ExecuteAsync(args);
                NetworkHelper.IsServerUp(url).Should().BeTrue($"Unable to connect to kestrel server - {KestrelStandalone} @ {url}");
                NetworkHelper.TestGetRequest(url, args);
            }
            finally
            {
                runCommand.KillTree();
            }
        }
    }
}
