using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.ShellShim
{
    internal class ShellShimPath
    {

        private static DirectoryPath _shimsDirectory;

        public ShellShimPath(DirectoryPath shimsDirectory)
        {
            _shimsDirectory = shimsDirectory;
        }

        public IEnumerable<FilePath> GetShimFiles(string commandName)
        {
            if (String.IsNullOrEmpty(commandName))
            {
                yield break;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return GetWindowsShimPath(commandName);
                yield return GetWindowsConfigPath(commandName);
            }
            else
            {
                yield return GetPosixShimPath(commandName);
            }
        }

        public FilePath GetPosixShimPath(string commandName)
        {
            return _shimsDirectory.WithFile(commandName);
        }

        public FilePath GetWindowsShimPath(string commandName)
        {
            return new FilePath(_shimsDirectory.WithFile(commandName).Value + ".exe");
        }

        public FilePath GetWindowsConfigPath(string commandName)
        {
            return new FilePath(GetWindowsShimPath(commandName).Value + ".config");
        }
    }
}
