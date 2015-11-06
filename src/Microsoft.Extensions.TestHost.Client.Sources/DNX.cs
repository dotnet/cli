// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Dnx.TestHost.Client
{
    public static class DNX
    {
        public static string FindDnx()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = "/c where dnx",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            process.Start();
            process.WaitForExit();
            // Get the first result only. Workaround for multiple results until IRuntimeEnvironment
            // exposes the path to DNX
            return process.StandardOutput.ReadLine().TrimEnd('\r', '\n');
        }

        public static string FindDnxDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dnx\\runtimes");

            if (Directory.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }
    }
}
