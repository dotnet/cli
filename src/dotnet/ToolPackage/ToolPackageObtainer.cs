using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.DotNet.Tools;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.Extensions.EnvironmentAbstractions;
using NuGet.ProjectModel;
using System.Transactions;

namespace Microsoft.DotNet.ToolPackage
{
    internal class ToolPackageObtainer: IToolPackageObtainer
    {
        private readonly Lazy<string> _bundledTargetFrameworkMoniker;
        private readonly Func<FilePath> _getTempProjectPath;
        private readonly IProjectRestorer _projectRestorer;
        private readonly DirectoryPath _toolsPath;
        private readonly DirectoryPath _offlineFeedPath;

        public ToolPackageObtainer(
            DirectoryPath toolsPath,
            DirectoryPath offlineFeedPath,
            Func<FilePath> getTempProjectPath,
            Lazy<string> bundledTargetFrameworkMoniker,
            IProjectRestorer projectRestorer
        )
        {
            _getTempProjectPath = getTempProjectPath;
            _bundledTargetFrameworkMoniker = bundledTargetFrameworkMoniker;
            _projectRestorer = projectRestorer ?? throw new ArgumentNullException(nameof(projectRestorer));
            _toolsPath = toolsPath;
            _offlineFeedPath = offlineFeedPath;
        }

        public ObtainTransaction CreateObtainTransaction(
            string packageId,
            string packageVersion = null,
            FilePath? nugetconfig = null,
            string targetframework = null,
            string source = null)
        {
            return new ObtainTransaction(packageId,
                packageVersion,
                nugetconfig,
                targetframework,
                source,
                _bundledTargetFrameworkMoniker,
                _getTempProjectPath,
                _projectRestorer,
                _toolsPath,
                _offlineFeedPath);
        }
    }
}
