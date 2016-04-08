﻿using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.PlatformAbstractions
{
    // This is to support some legacy stuff.
    // dnu restore (and thus dotnet restore) always uses win7-x64 as the Windows restore target,
    // so, when picking targets out of the lock file, we need to do version fallback since the
    // active RID might be higher than the RID in the lock file.
    //
    // We should clean this up. Filed #619 to track.
    public static class RuntimeEnvironmentRidExtensions
    {
        // Work around NuGet/Home#1941
        public static IEnumerable<string> GetOverrideRestoreRuntimeIdentifiers(this IRuntimeEnvironment env)
        {
            if (env.OperatingSystemPlatform != Platform.Windows)
            {
                yield return env.GetRuntimeIdentifier();
            }
            else
            {
                yield return "win7-x86";
                yield return "win7-x64";
            }
        }

        // Gets the identfier that is used for restore by default (this is different from the actual RID, but only on Windows)
        public static string GetLegacyRestoreRuntimeIdentifier(this IRuntimeEnvironment env)
        {
            if (env.OperatingSystemPlatform != Platform.Windows)
            {
                return env.GetRuntimeIdentifier();
            }
            else
            {
                var arch = env.RuntimeArchitecture.ToLowerInvariant();
                return "win7-" + arch;
            }
        }

        public static IEnumerable<string> GetAllCandidateRuntimeIdentifiers(this IRuntimeEnvironment env)
        {
            if (env.OperatingSystemPlatform != Platform.Windows)
            {
                yield return env.GetRuntimeIdentifier();
            }
            else
            {
                var arch = env.RuntimeArchitecture.ToLowerInvariant();
                if (env.OperatingSystemVersion.StartsWith("6.1", StringComparison.Ordinal))
                {
                    yield return "win7-" + arch;
                }
                else if (env.OperatingSystemVersion.StartsWith("6.2", StringComparison.Ordinal))
                {
                    yield return "win8-" + arch;
                    yield return "win7-" + arch;
                }
                else if (env.OperatingSystemVersion.StartsWith("6.3", StringComparison.Ordinal))
                {
                    yield return "win81-" + arch;
                    yield return "win8-" + arch;
                    yield return "win7-" + arch;
                }
                else if (env.OperatingSystemVersion.StartsWith("10.0", StringComparison.Ordinal))
                {
                    yield return "win10-" + arch;
                    yield return "win81-" + arch;
                    yield return "win8-" + arch;
                    yield return "win7-" + arch;
                }
            }
        }
    }
}
