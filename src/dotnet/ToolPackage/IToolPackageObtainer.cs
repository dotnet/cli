using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal interface IToolPackageObtainer
    {
        ToolConfigurationAndExecutableDirectory ObtainAndReturnExecutablePath(
            string packageId, 
            string packageVersion = null, 
            FilePath? nugetconfig = null, 
            string targetframework = null);
    }
}
