// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.ProjectModel;
using NuGet;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Pack
{
    public static class PackageBuilderExtensions
    {
        public static void TryAddIntellisenseFiles(this PackageBuilder packageBuilder, ProjectContext context, string outputPath,
            Project project)
        {
            packageBuilder.TryAddOutputFile(context, outputPath, $"{project.Name}.xml");
        }

        public static void TryAddSymbolFiles(this PackageBuilder packageBuilder, ProjectContext context, string outputPath,
            Project project)
        {
            TryAddOutputFile(packageBuilder, context, outputPath, $"{project.Name}.pdb");
            TryAddOutputFile(packageBuilder, context, outputPath, $"{project.Name}.mdb");
        }

        public static void TryAddCommandFiles(this PackageBuilder packageBuilder, ProjectContext context, string outputPath,
            Project project)
        {
            packageBuilder.TryAddOutputFile(context, outputPath, $"{project.Name}.exe");
            packageBuilder.TryAddOutputFile(context, outputPath, $"{project.Name}.deps");
        }

        internal static void TryAddOutputFile(this PackageBuilder packageBuilder,
                                             ProjectContext context,
                                             string outputPath,
                                             string filePath)
        {
            var targetPath = Path.Combine("lib", context.TargetFramework.GetTwoDigitShortFolderName(), Path.GetFileName(filePath));
            var sourcePath = Path.Combine(outputPath, filePath);

            if (!File.Exists(sourcePath))
            {
                return;
            }

            packageBuilder.Files.Add(new PhysicalPackageFile
            {
                SourcePath = sourcePath,
                TargetPath = targetPath
            });
        }
    }
}
