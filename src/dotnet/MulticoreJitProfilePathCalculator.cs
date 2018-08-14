// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Cli
{
    internal class MulticoreJitProfilePathCalculator
    {
        private string _multicoreJitProfilePath;

        public string MulticoreJitProfilePath
        {
            get
            {
                if (_multicoreJitProfilePath == null)
                {
                    CalculateProfileRootPath();
                }

                return _multicoreJitProfilePath;
            }
        }

        private void CalculateProfileRootPath()
        {
            var profileRoot = GetRuntimeDataRootPathString();

            var version = Product.Version;

            var rid = PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();

            _multicoreJitProfilePath = Path.Combine(profileRoot, "optimizationdata", version, rid);
        }

        private string GetRuntimeDataRootPathString()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsRuntimeDataRoot();

            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetOSXRuntimeDataRoot();
            
            else
                return GetUnixRuntimeDataRoot();
        }

        private static string GetWindowsRuntimeDataRoot()
        {
            return $@"{(Environment.GetEnvironmentVariable("LocalAppData"))}\Microsoft\dotnet\";
        }

        private static string GetOSXRuntimeDataRoot()
        {
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library/dotnet");
        }

        private static string GetUnixRuntimeDataRoot()
        {
            var XDG_DATA_HOME = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

            if(string.IsNullOrEmpty(XDG_DATA_HOME))
                XDG_DATA_HOME = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".local/share");
            
            return Path.Combine(XDG_DATA_HOME, "dotnet");
        }
    }
}
