﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli;

namespace Microsoft.DotNet.Tools.MSBuild
{
    public class MSBuildForwardingApp
    {
        private const string s_msbuildExeName = "MSBuild.dll";
        private readonly ForwardingApp _forwardingApp;

        public MSBuildForwardingApp(IEnumerable<string> argsToForward)
        {
            _forwardingApp = new ForwardingApp(
                GetMSBuildExePath(),
                argsToForward,
                environmentVariables: GetEnvironmentVariables());
        }

        public int Execute()
        {
            return _forwardingApp.Execute();
        }

        private static Dictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>
            {
                { "MSBuildExtensionsPath", AppContext.BaseDirectory },
                { "CscToolExe", GetRunCscPath() }
            };
        }

        private static string GetMSBuildExePath()
        {
            return Path.Combine(
                AppContext.BaseDirectory,
                s_msbuildExeName);
        }

        private static string GetRunCscPath()
        {
            var scriptExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            return Path.Combine(AppContext.BaseDirectory, $"RunCsc{scriptExtension}");
        }
    }
}
