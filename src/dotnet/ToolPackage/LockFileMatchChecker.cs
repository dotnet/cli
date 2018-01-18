// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.ToolPackage
{
    internal class LockFileMatchChecker
    {
        private readonly string[] _pathInLockFilePathInArray;
        private readonly string[] _entryPointPathInArray;

        /// <summary>
        /// Check if LockFileItem matches the targetRelativeFilePath.
        /// The path in LockFileItem is in pattern tools/TFM/RID/my/tool.dll, tools/TFM/RID is selected by NuGet.
        /// And there will be only one TFM/RID combination.
        /// When "my/tools.dll" part matches excatly with the targetRelativeFilePath, return true. 
        /// </summary>
        /// <param name="lockFileItem">LockFileItem from asset.json restored from temp project</param>
        /// <param name="targetRelativeFilePath">file path relative to tools/TFM/RID</param>
        public LockFileMatchChecker(LockFileItem lockFileItem, string targetRelativeFilePath)
        {
            _pathInLockFilePathInArray = SplitPathByDirectorySeparator(lockFileItem.Path);
            _entryPointPathInArray = SplitPathByDirectorySeparator(targetRelativeFilePath);
        }

        internal bool Matches()
        {
            return (_entryPointPathInArray.Length >= 1)
                   && PathInLockFileDirectoriesStartWithToolsAndFollowsTwoSubFolder()
                   && SubPathMatchesTargetFilePath();
        }

        private bool SubPathMatchesTargetFilePath()
        {
            string[] pathAfterToolsTfmRid = _pathInLockFilePathInArray.Skip(3).ToArray();
            return !pathAfterToolsTfmRid
                .Where((directoryOnEveryLevel, i) => directoryOnEveryLevel != _entryPointPathInArray[i])
                .Any();
        }

        private bool PathInLockFileDirectoriesStartWithToolsAndFollowsTwoSubFolder()
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

        private static string[] SplitPathByDirectorySeparator(string path)
        {
            return path.Split('\\', '/');
        }
    }
}
