// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build.UploadToLinuxPackageRepository
{
    internal interface IAzurelinuxRepositoryServiceHttpStrategy
    {
        Task<string> Execute(HttpClient client, Uri baseAddress);
    }
}
