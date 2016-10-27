// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.Build
{
    public class DotNetPack3 : DotNetTool
    {
        protected override string Command
        {
            get { return "pack3"; }
        }

        protected override string Args
        {
            get { return $"{GetProjectPath()} {GetConfiguration()} {GetNoBuild()} {GetOutput()} {GetVersionSuffix()}"; }
        }

        public string Configuration { get; set; }

        public bool NoBuild { get; set; }
        
        public string Output { get; set; }

        public string ProjectPath { get; set; }

        public string VersionSuffix { get; set; }

        private string GetConfiguration()
        {
            if (!string.IsNullOrEmpty(Configuration))
            {
                return $"--configuration {Configuration}";
            }

            return null;
        }

        private string GetNoBuild()
        {
            if (NoBuild)
            {
                return $"--no-build";
            }

            return null;
        }
        
        private string GetOutput()
        {
            if (!string.IsNullOrEmpty(Output))
            {
                return $"--output {Output}";
            }

            return null;
        }

        private string GetProjectPath()
        {
            if (!string.IsNullOrEmpty(ProjectPath))
            {
                return $"{ProjectPath}";
            }

            return null;
        }

        private string GetVersionSuffix()
        {
            if (!string.IsNullOrEmpty(VersionSuffix))
            {
                return $"--version-suffix {VersionSuffix}";
            }

            return null;
        }
    }
}
