// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Pack
{
    public static class ProjectExtensions
    {
        public static bool IsCommand(this Project project, NuGetFramework targetFramework, string configurationName)
        {
            return project.Name.StartsWith("dotnet-") &&
                   project.GetCompilerOptions(targetFramework, configurationName)
                       .EmitEntryPoint.GetValueOrDefault();
        }
    }
}
