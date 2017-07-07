﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Tools.Test.Utilities;
using Xunit;
using FluentAssertions;

namespace Microsoft.DotNet.Tests
{
    public class MockTelemetry : ITelemetry
    {
        public bool Enabled { get; set; }

        public string EventName { get; set; }
        public IDictionary<string, string> Properties { get; set; }

        public void TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            EventName = eventName;
            Properties = properties;
        }
    }

    public class TelemetryCommandTests : TestBase
    {
        [Fact]
        public void TestProjectDependencyIsNotAvailableThroughDriver()
        {
            MockTelemetry mockTelemetry = new MockTelemetry();
            string[] args = { "help" };
            Microsoft.DotNet.Cli.Program.ProcessArgs(args, mockTelemetry);
            Assert.Equal(mockTelemetry.EventName, args[0]);
        }

        [WindowsOnlyFact]
        public void InternalreportinstallsuccessCommandCollectExeNameWithEventname()
        {
            MockTelemetry mockTelemetry = new MockTelemetry();
            string[] args = { "c:\\mypath\\dotnet-sdk-latest-win-x64.exe" };

            InternalReportinstallsuccess.ProcessInputAndSendTelemetry(args, mockTelemetry);

            mockTelemetry.EventName.Should().Be("reportinstallsuccess");
            mockTelemetry.Properties["exeName"].Should().Be("dotnet-sdk-latest-win-x64.exe");
        }

        [Fact]
        public void InternalreportinstallsuccessCommandIsRegistedInBuiltIn()
        {
            BuiltInCommandsCatalog.Commands.Should().ContainKey("internal-reportinstallsuccess");
        }
    }
}
