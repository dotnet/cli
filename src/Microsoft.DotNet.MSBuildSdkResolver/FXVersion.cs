// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.MSBuildSdkResolver
{
    // Note: This is SemVer 2.0.0 https://semver.org/spec/v2.0.0.html
    // See the original version of this code here: https://github.com/dotnet/core-setup/blob/master/src/corehost/cli/fxr/fx_ver.cpp
    internal sealed class FXVersion
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string Pre { get; }
        public string Build { get; }

        public FXVersion(int major, int minor, int patch, string pre = "", string build = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Pre = pre;
            Build = build;
        }

        public static int Compare(FXVersion s1, FXVersion s2)
        {
            if (s1.Major != s2.Major)
            {
                return s1.Major > s2.Major ? 1 : -1;
            }

            if (s1.Minor != s2.Minor)
            {
                return s1.Minor > s2.Minor ? 1 : -1;
            }

            if (s1.Patch != s2.Patch)
            {
                return s1.Patch > s2.Patch ? 1 : -1;
            }

            if (string.IsNullOrEmpty(s1.Pre) || string.IsNullOrEmpty(s2.Pre))
            {
                // Empty (release) is higher precedence than prerelease
                return string.IsNullOrEmpty(s1.Pre) ? (string.IsNullOrEmpty(s2.Pre) ? 0 : 1) : -1;
            }

            string [] ids1 = s1.Pre.Substring(1).Split('.');
            string [] ids2 = s2.Pre.Substring(1).Split('.');

            for (int i = 0; true ; ++i)
            {
                if ((i >= ids1.Length) || (i >= ids2.Length))
                {
                    // One or both of the identifier lists has terminated
                    // Longest list of identifiers has highest precedecnce
                    return ids1.Length == ids2.Length ? 0 : ids1.Length >= ids2.Length ? 1 : -1;
                }

                int preCompare = string.CompareOrdinal(ids1[i], ids2[i]);

                if (preCompare != 0)
                {
                    int n1;
                    bool b1 = int.TryParse(ids1[i], out n1);
                    int n2;
                    bool b2 = int.TryParse(ids2[i], out n2);

                    if (b1 && b2)
                    {
                        // Both are numeric use numeric ordering (note never equal)
                        return n1 > n2 ? 1 : -1;
                    }
                    else if (b1 || b2)
                    {
                        // Exactly one is numeric
                        // Numeric has lower precedence
                        return b2 ? 1 : -1;
                    }
                    else
                    {
                        // Both are alphanumeric, use ascii sort order
                        // Since we are using only ascii characters, unicode ordinal sort == ascii sort
                        return preCompare > 0 ? 1 : -1;
                    }
                }
            }
        }

        private static bool ValidIdentifier(string id, bool buildMeta)
        {
            if (string.IsNullOrEmpty(id))
            {
                // Identifier must not be empty
                return false;
            }

            if (id.FindFirstNotOf("-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", 0) != -1)
            {
                // ids must be of the set [0-9a-zA-Z.-]
                return false;
            }

            if (!buildMeta && id.Substring(0,1) == "0" && id.Length > 1 && id.FindFirstNotOf("0123456789", 1) == -1)
            {
                // numeric identifiers must not be padded with 0s
                return false;
            }
            return true;
        }

        private static bool ValidIdentifiers(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return true;
            }

            bool prerelease = ids.Substring(0, 1) == "-";
            bool buildMeta = ids.Substring(0, 1) == "+";

            if (!(prerelease || buildMeta))
            {
                // ids must start with '-' or '+' for prerelease & build respectively
                return false;
            }

            foreach (string id in ids.Substring(1).Split('.'))
            {
                if (!ValidIdentifier(id, buildMeta))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryParse(string fxVersionString, out FXVersion FXVersion)
        {
            FXVersion = null;
            if (string.IsNullOrEmpty(fxVersionString))
            {
                return false;
            }

            int majorSeparator = fxVersionString.IndexOf(".");
            if (majorSeparator == -1)
            {
                return false;
            }

            int major = 0;
            if (!int.TryParse(fxVersionString.Substring(0, majorSeparator), out major))
            {
                return false;
            }
            if (majorSeparator > 1 && fxVersionString.Substring(0, 1) == "0")
            {
                return false;
            }

            int minorStart = majorSeparator + 1;
            int minorSeparator = fxVersionString.IndexOf(".", minorStart);
            if (minorSeparator == -1)
            {
                return false;
            }

            int minor = 0;
            if (!int.TryParse(fxVersionString.Substring(minorStart, minorSeparator - minorStart), out minor))
            {
                return false;
            }
            if (minorSeparator - minorStart > 1 && fxVersionString.Substring(minorStart, 1) == "0")
            {
                return false;
            }

            int patch = 0;
            int patchStart = minorSeparator + 1;
            int patchSeparator = fxVersionString.FindFirstNotOf("0123456789", patchStart);
            if (patchSeparator == -1)
            {
                if (!int.TryParse(fxVersionString.Substring(patchStart), out patch))
                {
                    return false;
                }
                if (patchStart + 1 < fxVersionString.Length && fxVersionString.Substring(patchStart, 1) == "0")
                {
                    return false;
                }

                FXVersion = new FXVersion(major, minor, patch);
                return true;
            }

            if (!int.TryParse(fxVersionString.Substring(patchStart, patchSeparator - patchStart), out patch))
            {
                return false;
            }
            if (patchSeparator - patchStart > 1 && fxVersionString.Substring(patchStart, 1) == "0")
            {
                return false;
            }

            int preStart = patchSeparator;
            int preSeparator = fxVersionString.IndexOf("+", preStart);

            string pre = (preSeparator == -1) ? fxVersionString.Substring(preStart) : fxVersionString.Substring(preStart, preSeparator - preStart);

            if (!ValidIdentifiers(pre))
            {
                return false;
            }

            string build = "";
            if (preSeparator != -1)
            {
                build = fxVersionString.Substring(preSeparator);
                if (!ValidIdentifiers(build))
                {
                    return false;
                }
            }

            FXVersion = new FXVersion(major, minor, patch, pre, build);

            return true;
        }
    }
}
