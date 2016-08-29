// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Cli
{
    public class VSTestForwardingApp
    {
        private const string vstestExename = "vstest.console.dll";
        private readonly ForwardingApp _forwardingApp;

        public VSTestForwardingApp(IEnumerable<string> argsToForward)
        {
            _forwardingApp = new ForwardingApp(
                GetVSTestExePath(),
                argsToForward,
                environmentVariables: GetEnvironmentVariables()
                );
        }

        public int Execute()
        {
            return _forwardingApp.Execute();
        }

        private Dictionary<string, string> GetEnvironmentVariables()
        {
            return new Dictionary<string, string>
            {
                { "DotnetHostPath", GetHostPath() },
            };
        }

        private string GetHostPath()
        {
            return new Muxer().MuxerPath;
        }

        private string GetVSTestExePath()
        {
            return Path.Combine(AppContext.BaseDirectory, vstestExename);
        }
    }
}
