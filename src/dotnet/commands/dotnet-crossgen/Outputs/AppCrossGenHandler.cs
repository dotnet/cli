// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.Tools.CrossGen.Outputs
{
    public class AppCrossGenHandler : CrossGenHandler
    {
        public AppCrossGenHandler(
            string crossGenExe,
            string diaSymReaderDll,
            CrossGenTarget crossGenTarget,
            DependencyContext depsFileContext,
            DependencyContext runtimeContext,
            string appDir,
            string outputDir,
            bool generatePDB)
            : base(crossGenExe, diaSymReaderDll, crossGenTarget, depsFileContext, runtimeContext, appDir, outputDir, generatePDB)
        {
        }

        protected override string GetOutputDirFor(string sourcePathUsed, RuntimeLibrary lib, string assetPath)
        {
            var sourceDir = Path.GetDirectoryName(sourcePathUsed);
            var sourceRelativeDir = sourceDir.Substring(AppDir.Length);
            var i = 0;
            for (; i < sourceRelativeDir.Length; i++)
            {
                if (sourceRelativeDir[i] != Path.AltDirectorySeparatorChar && sourceRelativeDir[i] != Path.DirectorySeparatorChar)
                {
                    break;
                }
            }
            return Path.Combine(OutputRoot, sourceRelativeDir.Substring(i));
        }

        protected override void OnCrossGenCompleted()
        {
            // copy over any asset that wasn't generated during the CrossGen process
            CopyOverDir(new string[] {});
        }

        // This method is written for simplicity not efficiency
        private void CopyOverDir(IEnumerable<string> relativePath)
        {
            var sourceDirStack = new string[] { AppDir }.Concat(relativePath).ToArray();
            var sourceDir = Path.Combine(sourceDirStack);
            var destDirStack = new string[] { OutputRoot }.Concat(relativePath).ToArray();
            var destDir = Path.Combine(destDirStack);

            if (!Directory.Exists(destDir))
            {
                Reporter.Verbose.WriteLine($"CrossGenTool wrap up: creating directory {destDir}");
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFileName = Path.Combine(destDir, fileName);
                if (!File.Exists(destFileName))
                {
                    Reporter.Verbose.WriteLine($"CrossGenTool wrap up: copying over file {destFileName}");
                    File.Copy(file, destFileName);
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                CopyOverDir(relativePath.Concat(new string[] {dirName} ));
            }
        }
    }
}