using System.Reflection;
using System.Runtime.Versioning;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli
{
    internal static class BundledTargetFramework
    {
        public static string TargetFrameworkMoniker
        {
            get
            {
                var targetFrameworkAttribute = typeof(BundledTargetFramework)
                    .GetTypeInfo()
                    .Assembly
                    .GetCustomAttribute<TargetFrameworkAttribute>();

                return NuGetFramework
                    .Parse(targetFrameworkAttribute.FrameworkName)
                    .GetShortFolderName();
            }
        }
    }
}
