﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.Build
{
    public class DotNetRestore3 : DotNetTool
    {
        protected override string Command
        {
            get { return "restore3"; }
        }

        protected override string Args
        {
            get { return $"{Root} {GetSource()} {GetPackages()} {GetSkipInvalidConfigurations()} {MSBuildArgs}"; }
        }

        public string Root { get; set; }

        public string Source { get; set; }

        public string Packages { get; set; }

        public bool SkipInvalidConfigurations { get; set; }

        public string MSBuildArgs { get; set; }

        private string GetSource()
        {
            if (!string.IsNullOrEmpty(Source))
            {
                return $"--source {Source}";
            }

            return null;
        }

        private string GetPackages()
        {
            if (!string.IsNullOrEmpty(Packages))
            {
                return $"--packages {Packages}";
            }

            return null;
        }

        private string GetSkipInvalidConfigurations()
        {
            if (SkipInvalidConfigurations)
            {
                return "/p:SkipInvalidConfigurations=true";
            }

            return null;
        }
    }
}
