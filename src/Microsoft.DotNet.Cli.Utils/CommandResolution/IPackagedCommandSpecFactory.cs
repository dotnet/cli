using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.Cli.Utils
{
    public interface IPackagedCommandSpecFactory
    {
        CommandSpec CreateCommandSpecFromLibrary(
            LockFileLibrary library,
            string commandName,
            IEnumerable<string> commandArguments,
            IEnumerable<string> allowedExtensions,
            string nugetPackagesRoot,
            CommandResolutionStrategy commandResolutionStrategy,
            string depsFilePath);
    }
}
