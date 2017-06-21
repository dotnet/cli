﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Configurer
{
    public class DotnetFirstTimeUseConfigurer
    {
        private IReporter _reporter;
        private IEnvironmentProvider _environmentProvider;
        private INuGetCachePrimer _nugetCachePrimer;
        private INuGetCacheSentinel _nugetCacheSentinel;
        private IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
        private string _cliFallbackFolderPath;

        public DotnetFirstTimeUseConfigurer(
            INuGetCachePrimer nugetCachePrimer,
            INuGetCacheSentinel nugetCacheSentinel,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
            IEnvironmentProvider environmentProvider,
            IReporter reporter,
            string cliFallbackFolderPath)
        {
            _nugetCachePrimer = nugetCachePrimer;
            _nugetCacheSentinel = nugetCacheSentinel;
            _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
            _environmentProvider = environmentProvider;
            _reporter = reporter;
            _cliFallbackFolderPath = cliFallbackFolderPath;
        }

        public void Configure()
        {
            if (ShouldPrintFirstTimeUseNotice())
            {
                PrintFirstTimeUseNotice();
            }

            if (ShouldPrimeNugetCache())
            {
                if (_nugetCacheSentinel.UnauthorizedAccess)
                {
                    PrintUnauthorizedAccessMessage();
                }

                PrintNugetCachePrimeMessage();

                _nugetCachePrimer.PrimeCache();
            }
        }

        private bool ShouldPrintFirstTimeUseNotice()
        {
            var showFirstTimeUseNotice =
                _environmentProvider.GetEnvironmentVariableAsBool("DOTNET_PRINT_TELEMETRY_MESSAGE", true);

            return ShouldRunFirstRunExperience() &&
                showFirstTimeUseNotice &&
                !_firstTimeUseNoticeSentinel.Exists();
        }

        private void PrintFirstTimeUseNotice()
        {
            _reporter.WriteLine();
            _reporter.WriteLine(LocalizableStrings.FirstTimeWelcomeMessage);

            _firstTimeUseNoticeSentinel.CreateIfNotExists();
        }

        private void PrintUnauthorizedAccessMessage()
        {
            _reporter.WriteLine();
            _reporter.WriteLine(string.Format(
                LocalizableStrings.UnauthorizedAccessMessage,
                _cliFallbackFolderPath));
        }

        private bool ShouldPrimeNugetCache()
        {
            return ShouldRunFirstRunExperience() &&
                !_nugetCachePrimer.SkipPrimingTheCache();
        }

        private void PrintNugetCachePrimeMessage()
        {
            string cachePrimeMessage = LocalizableStrings.NugetCachePrimeMessage;
            _reporter.WriteLine();
            _reporter.WriteLine(cachePrimeMessage);
        }

        private bool ShouldRunFirstRunExperience()
        {
            var skipFirstTimeExperience = 
                _environmentProvider.GetEnvironmentVariableAsBool("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", false);

            return !skipFirstTimeExperience &&
                !_nugetCacheSentinel.Exists() &&
                !_nugetCacheSentinel.InProgressSentinelAlreadyExists();
        }
    }
}
