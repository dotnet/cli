// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Tools
{
    internal class MsbuildUtilities
    {
        public static void ConvertPathsToRelative(string baseDirectory, ref List<string> paths)
        {
            paths = paths.Select((p) => 
                PathUtility.GetRelativePath(baseDirectory, Path.GetFullPath(p))).ToList();
        }

        public static string NormalizeSlashes(string path)
        {
            return path.Replace('/', '\\');
        }

        public static void EnsureAllPathsExist(List<string> paths, string errorFormatString)
        {
            var notExisting = new List<string>();
            foreach (var p in paths)
            {
                if (!File.Exists(p))
                {
                    notExisting.Add(p);
                }
            }

            if (notExisting.Count > 0)
            {
                throw new GracefulException(
                    string.Join(
                        Environment.NewLine,
                        notExisting.Select((p) => string.Format(errorFormatString, p))));
            }
        }

    }
}
