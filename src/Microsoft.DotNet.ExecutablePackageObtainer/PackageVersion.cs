// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.DotNet.ExecutablePackageObtainer
{
    internal class PackageVersion
    {
        public PackageVersion(string packageVersion)
        {
            if (packageVersion == null)
            {
                Value = Path.GetRandomFileName();
                IsPlaceHolder = true;
            }
            else
            {
                Value = packageVersion;
                IsPlaceHolder = false;
            }
        }

        public bool IsPlaceHolder { get; }
        public string Value { get; }
        public bool IsConcreteValue => !IsPlaceHolder;
    }
}
