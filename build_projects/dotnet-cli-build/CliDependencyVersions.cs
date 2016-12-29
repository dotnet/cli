using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class CliDependencyVersions
    {
        public static readonly string SharedFrameworkVersion = "1.0.4-servicing-004628-00";
        public static readonly string SharedHostVersion = "DEPENDENCY 'Microsoft.NETCore.DotNetHost' NOT FOUND";
        public static readonly string HostFxrVersion = "DEPENDENCY 'Microsoft.NETCore.DotNetHostResolver' NOT FOUND";

        public static readonly string SharedFrameworkChannel = "preview";
        public static readonly string SharedHostChannel = "preview";
        public static readonly string HostFxrChannel = "preview";
    }
}
