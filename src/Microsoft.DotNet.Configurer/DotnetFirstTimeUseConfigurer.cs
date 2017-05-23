// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Configurer
{
    public class DotnetFirstTimeUseConfigurer
    {
        private IEnvironmentProvider _environmentProvider;
        private INuGetCachePrimer _nugetCachePrimer;
        private INuGetCacheSentinel _nugetCacheSentinel;

        public DotnetFirstTimeUseConfigurer(
            INuGetCachePrimer nugetCachePrimer,
            INuGetCacheSentinel nugetCacheSentinel,
            IEnvironmentProvider environmentProvider)
        {
            _nugetCachePrimer = nugetCachePrimer;
            _nugetCacheSentinel = nugetCacheSentinel;
            _environmentProvider = environmentProvider;
        }

        public void Configure()
        {
            if(ShouldPrimeNugetCache())
            {
                PrintFirstTimeUseNotice();

                _nugetCachePrimer.PrimeCache();
            }
        }

        private void PrintFirstTimeUseNotice()
        {
            const string firstTimeUseWelcomeMessage = LocalizableStrings.FirstTimeUseWelcomeMessage;
            const string firstTimeUseTelemetryEnabledMessage = LocalizableStrings.FirstTimeUseTelemetryEnabledMessage;
            const string firstTimeUseConfiguringMessage = LocalizableStrings.FirstTimeUseConfiguringMessage;

            Reporter.Output.WriteLine();
            Reporter.Output.WriteLine(firstTimeUseWelcomeMessage)
            
            if (IsTelemetryEnabled()) {
                Reporter.Output.WriteLine();
                Reporter.Output.WriteLine(firstTimeUseTelemetryEnabledMessage);
            }

            Reporter.Output.WriteLine();
            Reporter.Output.WriteLine(firstTimeUseConfiguringMessage);
        }

        private bool ShouldPrimeNugetCache()
        {
            var skipFirstTimeExperience = 
                _environmentProvider.GetEnvironmentVariableAsBool("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", false);

            return !skipFirstTimeExperience &&
                !_nugetCacheSentinel.Exists() &&
                !_nugetCacheSentinel.InProgressSentinelAlreadyExists();
        }

        private bool IsTelemetryEnabled()
        {
            var cliTelemetryOptout = 
                _environmentProvider.GetEnvironmentVariableAsBool("DOTNET_CLI_TELEMETRY_OPTOUT", false);

            return !cliTelemetryOptout;
        }
    }
}
