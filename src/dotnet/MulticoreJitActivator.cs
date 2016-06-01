// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;    
using System.Runtime.Loader;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.Common;
using Microsoft.Extensions.PlatformAbstractions;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Cli
{
    public class MulticoreJitActivator
    {
        public bool TryActivateMulticoreJit()
        {
            var disableMulticoreJit = IsMulticoreJitDisabled();
                
            if (disableMulticoreJit)
            {
                return false;
            }

            StartCliProfileOptimization();
            
            return true;
        }
        
        private bool IsMulticoreJitDisabled()
        {
            return Env.GetEnvironmentVariableAsBool("DOTNET_DISABLE_MULTICOREJIT");
        }
        
        private void StartCliProfileOptimization()
        {
            var profileOptimizationRootPath = new MulticoreJitProfilePathCalculator().MulticoreJitProfilePath;

            if (!TryEnsureDirectory(profileOptimizationRootPath))
            {
                return;
            }
            
            AssemblyLoadContext.Default.SetProfileOptimizationRoot(profileOptimizationRootPath);
            
            AssemblyLoadContext.Default.StartProfileOptimization("dotnet");
        }

        private bool TryEnsureDirectory(string directoryPath)
        {
            try
            {
                PathUtility.EnsureDirectory(directoryPath);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
