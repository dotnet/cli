using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    public interface ICanRestoreProject
    {
        void Restore(
            FilePath tempProjectPath,
            DirectoryPath assetJsonOutput, 
            FilePath nugetconfig);
    }
}
