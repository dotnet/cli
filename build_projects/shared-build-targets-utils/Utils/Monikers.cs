using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public class Monikers
    {
        public static string GetBadgeMoniker()
        {
            switch (RuntimeEnvironment.GetRuntimeIdentifier())
            {
                case "ubuntu.14.04-x64":
                    return "Ubuntu_x64";
                case "ubuntu.16.04-x64":
                    return "Ubuntu_16_04_x64";
                case "ubuntu.18.04-x64":
                    return "Ubuntu_18_04_x64";
                case "fedora.24-x64":
                    return "Fedora_24_x64";
                case "fedora.27-x64":
                    return "Fedora_27_x64";
                case "fedora.28-x64":
                    return "Fedora_28_x64";
                case "opensuse.42.1-x64":
                    return "openSUSE_42_1_x64";
                case "opensuse.42.3-x64":
                    return "openSUSE_42_3_x64";
                case "rhel.7.2-x64":
                    return "RHEL_x64";
                case "centos.7-x64":
                    return "CentOS_x64";
                case "debian.8-x64":
                    return "Debian_x64";
            }

            return $"{CurrentPlatform.Current}_{CurrentArchitecture.Current}";
        }
        
        public static string GetOSShortName()
        {
            string osname = "";
            switch (CurrentPlatform.Current)
            {
                case BuildPlatform.Windows:
                    osname = "win";
                    break;
                default:
                    osname = CurrentPlatform.Current.ToString().ToLower();
                    break;
            }

            return osname;
        }
    }
}
