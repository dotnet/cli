// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.DotNet.ToolPackage
{
    internal class LockFileMatcher
    {
        private static string[] _pathInLockFilePathInArray;
        private static string[] _entryPointPathInArray;

        internal static bool MatchesFile(string pathInLockFile, string entryPoint)
        {
            _pathInLockFilePathInArray = SplitPathByDirectorySeparator(pathInLockFile);
            _entryPointPathInArray = SplitPathByDirectorySeparator(entryPoint);

            return EntryPointHasFileAtLeast()
                   && PathInLockFileHasExcatlyToolsTfmRidAsParentDirectories()
                   && RestMatchsOtherThanPathInLockFileHasToolsTfmRidAsParentDirectories();
        }

        private static bool RestMatchsOtherThanPathInLockFileHasToolsTfmRidAsParentDirectories()
        {
            string[] pathAfterToolsTfmRid = _pathInLockFilePathInArray.Skip(3).ToArray();
            return !pathAfterToolsTfmRid
                .Where((directoryOnEveryLevel, i) => directoryOnEveryLevel != _entryPointPathInArray[i])
                .Any();
        }

        private static bool PathInLockFileHasExcatlyToolsTfmRidAsParentDirectories()
        {
            if (_pathInLockFilePathInArray.Length - _entryPointPathInArray.Length != 3)
            {
                return false;
            }

            if (_pathInLockFilePathInArray[0] != "tools")
            {
                return false;
            }

            return true;
        }

        private static bool EntryPointHasFileAtLeast()
        {
            return _entryPointPathInArray.Length >= 1;
        }

        private static string[] SplitPathByDirectorySeparator(string path)
        {
            return path.Split('\\', '/');
        }
    }
}
