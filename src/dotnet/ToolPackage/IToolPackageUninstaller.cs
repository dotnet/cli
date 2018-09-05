using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ToolPackage
{
    internal interface IToolPackageUninstaller
    {
        void Uninstall(DirectoryPath packageDirectory);
    }
}
