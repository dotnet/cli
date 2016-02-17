﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET451
using System.Runtime.Loader;
#endif
using System.Reflection;
using System.Text;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Utilities
{
    public static class VersionUtility
    {
        public static readonly string DnxCoreFrameworkIdentifier = "DNXCore";
        public static readonly string DnxFrameworkIdentifier = "DNX";
        public static readonly string NetPlatformFrameworkIdentifier = ".NETPlatform";
        public static readonly string NetFrameworkIdentifier = ".NETFramework";

        internal static NuGetVersion GetAssemblyVersion(string path)
        {
#if NET451
            return new NuGetVersion(AssemblyName.GetAssemblyName(path).Version);
#else
            return new NuGetVersion(AssemblyLoadContext.GetAssemblyName(path).Version);
#endif
        }

        public static string RenderVersion(VersionRange range)
        {
            if (range == null)
            {
                return null;
            }

            if (range.MinVersion == range.MaxVersion &&
                (range.Float == null || range.Float.FloatBehavior == NuGetVersionFloatBehavior.None))
            {
                return range.MinVersion.ToNormalizedString();
            }
            var sb = new StringBuilder();
            sb.Append(">= ");
            switch (range?.Float?.FloatBehavior)
            {
                case null:
                case NuGetVersionFloatBehavior.None:
                    sb.Append(range.MinVersion.ToNormalizedString());
                    break;
                case NuGetVersionFloatBehavior.Prerelease:
                    // Work around nuget bug: https://github.com/NuGet/Home/issues/1598
                    // sb.AppendFormat("{0}-*", range.MinVersion);
                    sb.Append($"{range.MinVersion.Version.Major}.{range.MinVersion.Version.Minor}.{range.MinVersion.Version.Build}");
                    if (string.IsNullOrEmpty(range.MinVersion.Release) || 
                        string.Equals("-", range.MinVersion.Release))
                    {
                        sb.Append($"-*");
                    }
                    else
                    {
                        sb.Append($"-{range.MinVersion.Release}*");
                    }
                    break;
                case NuGetVersionFloatBehavior.Revision:
                    sb.Append($"{range.MinVersion.Version.Major}.{range.MinVersion.Version.Minor}.{range.MinVersion.Version.Build}.*");
                    break;
                case NuGetVersionFloatBehavior.Patch:
                    sb.Append($"{range.MinVersion.Version.Major}.{range.MinVersion.Version.Minor}.*");
                    break;
                case NuGetVersionFloatBehavior.Minor:
                    sb.AppendFormat($"{range.MinVersion.Version.Major}.*");
                    break;
                case NuGetVersionFloatBehavior.Major:
                    sb.AppendFormat("*");
                    break;
                default:
                    break;
            }

            if (range.MaxVersion != null)
            {
                sb.Append(range.IsMaxInclusive ? " <= " : " < ");
                sb.Append(range.MaxVersion);
            }

            return sb.ToString();
        }
    }
}
