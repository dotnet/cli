using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Graph;

namespace Microsoft.DotNet.Cli.Utils
{
    public interface IPackagedCommandSpecFactory
    {
        CommandSpec CreateCommandSpecFromLibrary(
            LockFilePackageLibrary library,
            string commandName,
            IEnumerable<string> commandArguments,
            IEnumerable<string> allowedExtensions,
            string nugetPackagesRoot,
            CommandResolutionStrategy commandResolutionStrategy,
            string depsFilePath);
        
    }
}
