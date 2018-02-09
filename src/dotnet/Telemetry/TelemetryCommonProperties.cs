// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.PlatformAbstractions;
using System.IO;
using System.Security;
using Microsoft.DotNet.Configurer;
using Microsoft.Win32;
using System.Linq;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Microsoft.DotNet.Cli.Telemetry
{
    internal class TelemetryCommonProperties
    {
        public TelemetryCommonProperties(
            Func<string> getCurrentDirectory = null,
            Func<string, string> hasher = null,
            Func<string> getMACAddress = null,
            IDockerContainerDetector dockerContainerDetector = null,
            IUserLevelCacheWriter userLevelCacheWriter = null)
        {
            _getCurrentDirectory = getCurrentDirectory ?? Directory.GetCurrentDirectory;
            _hasher = hasher ?? Sha256Hasher.Hash;
            _getMACAddress = getMACAddress ?? MacAddressGetter.GetMacAddress;
            _dockerContainerDetector = dockerContainerDetector ?? new DockerContainerDetectorForTelemetry();
            _userLevelCacheWriter = userLevelCacheWriter ?? new UserLevelCacheWriter(new CliFolderPathCalculator());
        }

        private readonly IDockerContainerDetector _dockerContainerDetector;
        private Func<string> _getCurrentDirectory;
        private Func<string, string> _hasher;
        private Func<string> _getMACAddress;
        private IUserLevelCacheWriter _userLevelCacheWriter;
        private const string OSVersion = "OS Version";
        private const string OSPlatform = "OS Platform";
        private const string RuntimeId = "Runtime Id";
        private const string ProductVersion = "Product Version";
        private const string TelemetryProfile = "Telemetry Profile";
        private const string CurrentPathHash = "Current Path Hash";
        private const string MachineId = "Machine ID";
        private const string DockerContainer = "Docker Container";
        private const string KernelVersion = "Kernel Version";
        private const string InstallationType = "Installation Type";
        private const string ProductType = "Product Type";
        private const string LibcRelease = "Libc Release";
        private const string LibcVersion = "Libc Version";

        private const string TelemetryProfileEnvironmentVariable = "DOTNET_CLI_TELEMETRY_PROFILE";
        private const string CannotFindMacAddress = "Unknown";

        private const string MachineIdCacheKey = "MachineId";
        private const string IsDockerContainerCacheKey = "IsDockerContainer";

        public Dictionary<string, string> GetTelemetryCommonProperties()
        {
            return new Dictionary<string, string>
            {
                {OSVersion, RuntimeEnvironment.OperatingSystemVersion},
                {OSPlatform, RuntimeEnvironment.OperatingSystemPlatform.ToString()},
                {RuntimeId, RuntimeEnvironment.GetRuntimeIdentifier()},
                {ProductVersion, Product.Version},
                {TelemetryProfile, Environment.GetEnvironmentVariable(TelemetryProfileEnvironmentVariable)},
                {DockerContainer, _userLevelCacheWriter.RunWithCache(IsDockerContainerCacheKey, () => _dockerContainerDetector.IsDockerContainer().ToString("G") )},
                {CurrentPathHash, _hasher(_getCurrentDirectory())},
                {MachineId, _userLevelCacheWriter.RunWithCache(MachineIdCacheKey, GetMachineId)},
                {KernelVersion, GetKernelVersion()},
                {InstallationType, GetInstallationType()},
                {ProductType, GetProductType()},
                {LibcRelease, GetLibcRelease()},
                {LibcVersion, GetLibcVersion()}
            };
        }

        private string GetMachineId()
        {
            var macAddress = _getMACAddress();
            if (macAddress != null)
            {
                return _hasher(macAddress);
            }
            else
            {
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Returns a string identifying the OS kernel.
        /// For Unix this currently comes from "uname -srv".
        /// For Windows this currently comes from RtlGetVersion().
        ///
        /// Here are some example values:
        ///
        ///     Alpine.36        Linux 4.9.60-linuxkit-aufs #1 SMP Mon Nov 6 16:00:12 UTC 2017
        ///     Centos.73        Linux 3.10.0-514.26.2.el7.x86_64 #1 SMP Tue Jul 4 15:04:05 UTC 2017
        ///     Debian.87        Linux 3.16.0-4-amd64 #1 SMP Debian 3.16.39-1+deb8u2 (2017-03-07)
        ///     Debian.90        Linux 4.9.0-2-amd64 #1 SMP Debian 4.9.18-1 (2017-03-30)
        ///     fedora.25        Linux 4.11.3-202.fc25.x86_64 #1 SMP Mon Jun 5 16:38:21 UTC 2017
        ///     Fedora.26        Linux 4.14.15-200.fc26.x86_64 #1 SMP Wed Jan 24 04:26:15 UTC 2018
        ///     Fedora.27        Linux 4.14.14-300.fc27.x86_64 #1 SMP Fri Jan 19 13:19:54 UTC 2018
        ///     OpenSuse.423     Linux 4.4.104-39-default #1 SMP Thu Jan 4 08:11:03 UTC 2018 (7db1912)
        ///     RedHat.69        Linux 2.6.32-696.20.1.el6.x86_64 #1 SMP Fri Jan 12 15:07:59 EST 2018
        ///     RedHat.72        Linux 3.10.0-514.21.1.el7.x86_64 #1 SMP Sat Apr 22 02:41:35 EDT 2017
        ///     RedHat.73        Linux 3.10.0-514.21.1.el7.x86_64 #1 SMP Sat Apr 22 02:41:35 EDT 2017
        ///     SLES.12          Linux 4.4.103-6.38-default #1 SMP Mon Dec 25 20:44:33 UTC 2017 (e4b9067)
        ///     suse.422         Linux 4.4.49-16-default #1 SMP Sun Feb 19 17:40:35 UTC 2017 (70e9954)
        ///     Ubuntu.1404      Linux 3.19.0-65-generic #73~14.04.1-Ubuntu SMP Wed Jun 29 21:05:22 UTC 2016
        ///     Ubuntu.1604      Linux 4.13.0-1005-azure #7-Ubuntu SMP Mon Jan 8 21:37:36 UTC 2018
        ///     Ubuntu.1604.WSL  Linux 4.4.0-43-Microsoft #1-Microsoft Wed Dec 31 14:42:53 PST 2014
        ///     Ubuntu.1610      Linux 4.8.0-45-generic #48-Ubuntu SMP Fri Mar 24 11:46:39 UTC 2017
        ///     Ubuntu.1704      Linux 4.10.0-19-generic #21-Ubuntu SMP Thu Apr 6 17:04:57 UTC 2017
        ///     Ubuntu.1710      Linux 4.13.0-25-generic #29-Ubuntu SMP Mon Jan 8 21:14:41 UTC 2018
        ///     OSX1012          Darwin 16.7.0 Darwin Kernel Version 16.7.0: Thu Jan 11 22:59:40 PST 2018; root:xnu-3789.73.8~1/RELEASE_X86_64
        ///     OSX1013          Darwin 17.4.0 Darwin Kernel Version 17.4.0: Sun Dec 17 09:19:54 PST 2017; root:xnu-4570.41.2~1/RELEASE_X86_64
        ///     Windows.10       Microsoft Windows 10.0.14393
        ///     Windows.10.Core  Microsoft Windows 10.0.14393
        ///     Windows.10.Nano  Microsoft Windows 10.0.14393
        ///     Windows.7        Microsoft Windows 6.1.7601 S
        ///     Windows.81       Microsoft Windows 6.3.9600
        /// </summary>
        private static string GetKernelVersion()
        {
            return RuntimeInformation.OSDescription;
        }

        /// For Windows, returns the OS installation type, eg. "Nano Server", "Server Core", "Server", or "Client".
        /// For Unix, currently returns empty string.
        /// </summary>
        private static string GetInstallationType()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform != Platform.Windows)
                return "";

            const string Key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            const string ValueName = @"InstallationType";

            try
            {
                return (string)Registry.GetValue(Key, ValueName, defaultValue: "");
            }
            // Catch everything: this is for telemetry only.
            catch (Exception e) 
            {
                Debug.Assert(e is ArgumentException | e is SecurityException | e is InvalidCastException);
                return "";
            }
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = false)]
        private static extern bool GetProductInfo(uint dwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out uint pdwReturnedProductType);

        /// <summary>
        /// For Windows, returns the product type, loosely the SKU, as encoded by GetProductInfo().
        /// For example, Enterprise is "4" (0x4) and Professional is "48" (0x30)
        /// See https://msdn.microsoft.com/en-us/library/windows/desktop/ms724358(v=vs.85).aspx for the full list.
        /// We're not attempting to decode the value on the client side as new Windows releases may add new values.
        /// For Unix, currently returns "4294967295" (uint.MaxValue).
        /// </summary>
        /// <returns></returns>
        private static string GetProductType()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform != Platform.Windows)
                return "0";

            try
            {
                if (GetProductInfo((uint)Environment.OSVersion.Version.Major, (uint)Environment.OSVersion.Version.Minor, 0, 0, out uint productType))
                {
                    return productType.ToString("D");
                }
            }
            // Catch everything: this is for telemetry only
            catch(Exception e)
            {
                Debug.Assert(false, $"Unexpected exception from GetProductInfo: ${e.GetType().Name}: ${e.Message}");
            }

            return uint.MaxValue.ToString("D"); // Error
        }

        [DllImport("libc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_release();

        [DllImport("libc", ExactSpelling = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gnu_get_libc_version();

        /// <summary>
        /// If gnulibc is available, returns the release, such as "stable".
        /// If the libc is musl, currently returns empty string.
        /// Otherwise returns empty string.
        /// </summary>
        private static string GetLibcRelease()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
                return "";

            try
            {
                return Marshal.PtrToStringUTF8(gnu_get_libc_release());
            }
            // Catch everything: this is for telemetry only
            catch (Exception e)
            {
                Debug.Assert(e is DllNotFoundException || e is EntryPointNotFoundException);
                return "";
            }
        }

        /// <summary>
        /// If gnulibc is available, returns the version, such as "2.22".
        /// If the libc is musl, currently returns empty string. (In future could run "ldd -version".)
        /// Otherwise returns empty string.
        /// </summary>
        private static string GetLibcVersion()
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
                return "";

            try
            {
                return Marshal.PtrToStringUTF8(gnu_get_libc_version());
            }
            // Catch everything: this is for telemetry only
            catch (Exception e)
            {
                Debug.Assert(e is DllNotFoundException || e is EntryPointNotFoundException);
                return "";
            }
        }
    }
}
